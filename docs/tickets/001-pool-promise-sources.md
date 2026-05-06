# CHOP-001: Pool SequentialHandlingPromiseSource to Eliminate GC Pressure

## Priority: 🔴 P0 — Critical
## Type: Performance / GC Optimization
## Component: Native/Messages/Handlers/Sources

---

## Problem Statement

Every message dispatch allocates a new `SequentialHandlingPromiseSource<TMessage, TContext>` on the heap. In a Unity game running at 60fps dispatching messages every frame, this creates ~60 allocations per second per message type — guaranteed GC pressure that causes frame hitches.

The codebase already acknowledges this with TODO comments:
```csharp
// BaseHandlingPromiseSource.cs line 8
// TODO :: Implement pooling for all handling promise source implementations.

// BaseMulticastHandler.cs line 44
// TODO :: Rent from the pool.
var source = new SequentialHandlingPromiseSource<TMessage, TContext>();
```

The `Dispose()` method on `BaseHandlingPromiseSource` already resets all state — it was **designed for pooling** but the pool was never implemented.

## Root Cause

**File:** `Native/Messages/Handlers/Multicast/BaseMulticastHandler.cs` (line 45)
```csharp
protected IHandlingPromiseSource InitiateWithSource(TMessage message, CancellationToken token)
{
    // ...
    var source = new SequentialHandlingPromiseSource<TMessage, TContext>();  // ALLOCATION
    source.Init(RegisteredMessageHandlers);
    source.Run(defaultContext);
    return source;
}
```

Each `SequentialHandlingPromiseSource` is approximately 72+ bytes including:
- Base class: 7 delegate fields (`OnCancelled`, `OnCompletion`, `OnFailure`, `OnNonSuccess`, `OnSuccess`, `FailureContext`, `InitiateDefaultContinuations`)
- Base class: `InnerSource`, `_isInitialized`
- Derived: `_isCompleted`, `_continuation`, `_currentAwaiter`, `_currentIndex`, `_context`, `_result`

## Solution Design

### Implementation

Create a generic `ObjectPool<T>` and integrate it into the promise source lifecycle:

```csharp
// New file: Native/Messages/System/ObjectPool.cs
namespace Chopsticks.Messages.System;

public sealed class ObjectPool<T> where T : class, new()
{
    private readonly ConcurrentBag<T> _pool;
    private readonly Action<T> _resetAction;
    private readonly int _maxSize;

    public ObjectPool(Action<T> resetAction, int maxSize = 64)
    {
        _pool = new ConcurrentBag<T>();
        _resetAction = resetAction;
        _maxSize = maxSize;
    }

    public T Rent()
    {
        if (_pool.TryTake(out var item))
            return item;
        return new T();
    }

    public void Return(T item)
    {
        if (_pool.Count >= _maxSize)
            return;  // Let GC collect overflow
        _resetAction(item);
        _pool.Add(item);
    }
}
```

### Integration Points

1. **Add pool to `BaseMulticastHandler`:**
```csharp
private static readonly ObjectPool<SequentialHandlingPromiseSource<TMessage, TContext>> _sourcePool = 
    new(source => source.Dispose(), maxSize: 32);
```

2. **Rent from pool in `InitiateWithSource`:**
```csharp
var source = _sourcePool.Rent();
source.Init(RegisteredMessageHandlers);
source.Run(defaultContext);
return source;
```

3. **Return to pool on completion in `SequentialHandlingPromiseSource.Step()`:**
```csharp
private void Step()
{
    if (_currentIndex == InnerSource!.Length)
    {
        _isCompleted = true;
        _continuation?.Invoke();
        // Return self to pool after all continuations run
        _returnToPool?.Invoke(this);
        return;
    }
    // ...
}
```

4. **Add return-to-pool callback:**
```csharp
// In BaseHandlingPromiseSource<T>
internal Action<IHandlingPromiseSource>? ReturnToPool { get; set; }
```

### Files to Modify

| File | Change |
|------|--------|
| `Native/Messages/System/ObjectPool.cs` | **NEW** — Generic object pool |
| `Native/Messages/Handlers/Sources/BaseHandlingPromiseSource.cs` | Add `ReturnToPool` callback, invoke in dispose |
| `Native/Messages/Handlers/Sources/SequentialHandlingPromiseSource.cs` | Invoke return-to-pool on completion |
| `Native/Messages/Handlers/Multicast/BaseMulticastHandler.cs` | Use pool instead of `new` |
| `Native/Messages/Handlers/Sources/IHandlingPromiseSource.cs` | Add `IDisposable` (see CHOP-014) |

---

## Acceptance Criteria

### Functional Requirements
- [ ] Message dispatch continues to work correctly with pooled sources
- [ ] All existing message handling tests pass without modification
- [ ] Promise sources are correctly reset before reuse (no stale state leaks)
- [ ] Pool overflow (beyond max size) gracefully falls back to GC
- [ ] Nested/recursive message dispatches work correctly (source re-entrancy)
- [ ] Concurrent message dispatches from multiple threads are safe

### Performance Requirements
- [ ] Zero heap allocations for message dispatch in steady-state (after pool warm-up)
- [ ] Pool warm-up allocates at most `maxSize` objects
- [ ] No lock contention in pool access under concurrent load
- [ ] Memory footprint of pool is bounded (maxSize * sizeof(source))

---

## Testing Specification

### Unit Tests

#### Test 1: Pool Rent and Return Cycle
```
GIVEN an empty ObjectPool<SequentialHandlingPromiseSource>
WHEN Rent() is called
THEN a new instance is created
AND WHEN Return() is called with that instance
AND WHEN Rent() is called again
THEN the same instance (by reference) is returned
```

#### Test 2: Pool Reset Clears State
```
GIVEN a SequentialHandlingPromiseSource that has completed handling
  WITH _isCompleted = true
  AND _result = HandlingResult.Success
  AND OnCompletion callback set
WHEN it is returned to the pool
AND rented again
THEN _isCompleted == false
AND _result == default
AND OnCompletion == null
AND OnCancelled == null
AND OnFailure == null
AND OnNonSuccess == null
AND OnSuccess == null
AND FailureContext == null
AND InnerSource == null
```

#### Test 3: Pool Overflow Handling
```
GIVEN a pool with maxSize = 4
AND 4 items already returned to the pool
WHEN a 5th item is returned
THEN it is NOT added to the pool (count remains 4)
AND the 5th item is eligible for GC
```

#### Test 4: Pool Under Concurrent Access
```
GIVEN a pool with maxSize = 16
WHEN 100 tasks concurrently Rent and Return sources
THEN no exceptions are thrown
AND pool count never exceeds maxSize
AND all rented instances are unique within a single Rent cycle
```

#### Test 5: Message Dispatch Uses Pool
```
GIVEN a MulticastContextHandler with 1 registered handler
WHEN TryHandle is called 100 times sequentially
THEN the number of SequentialHandlingPromiseSource allocations
  is <= maxPoolSize (warm-up) + 0 (steady state)
  (verified via allocation counter or mock pool)
```

#### Test 6: Recursive Message Dispatch
```
GIVEN Handler A that dispatches Message B in its handler
AND Handler B registered for Message B
WHEN Message A is dispatched
THEN both messages are handled correctly
AND no pool corruption occurs (source A is not returned until after B completes)
```

#### Test 7: Source Reuse After Async Completion
```
GIVEN a handler that completes asynchronously (after Task.Delay)
WHEN a message is dispatched
AND the handler completes after 10ms
THEN the source is returned to the pool after all continuations fire
AND the next dispatch correctly reuses the pooled source
```

#### Test 8: Pool Thread Safety Stress Test
```
GIVEN a pool with maxSize = 8
WHEN 50 threads each perform 1000 Rent/Return cycles
THEN no ConcurrentBag corruption
AND no duplicate references in circulation
AND final pool count <= maxSize
```

### Integration Tests

#### Test 9: Full Pipeline With Pool
```
GIVEN a complete multicast handler with interceptors
AND multiple registered handlers (sync and async)
WHEN 1000 messages are dispatched sequentially
THEN all messages produce correct HandlingResult
AND GC.GetTotalMemory shows no significant growth after warm-up
```

#### Test 10: Pool Behavior Under Cancellation
```
GIVEN a handler that checks CancellationToken
WHEN a message is dispatched with a pre-cancelled token
THEN the source is still returned to the pool
AND the result is HandlingResult.Cancelled
```

### Performance Benchmarks (BenchmarkDotNet)

#### Benchmark 1: Allocation Comparison
```
[Benchmark(Baseline = true)]
public HandlingResultPromise Dispatch_NoPool() => handler.TryHandle(message);

[Benchmark]
public HandlingResultPromise Dispatch_WithPool() => pooledHandler.TryHandle(message);

// Expected: WithPool shows 0 Gen0 collections after warm-up
```

#### Benchmark 2: Throughput Under Load
```
[Benchmark]
[Arguments(1, 10, 100, 1000)]
public void Dispatch_Concurrent(int parallelism)
// Expected: Linear throughput scaling with no GC pauses
```

---

## Risks & Mitigations

| Risk | Mitigation |
|------|-----------|
| Stale state leaks between reuses | Comprehensive Dispose() already resets all fields; add assertion in debug builds |
| Pool memory never released | Set reasonable maxSize; consider periodic trim or weak references |
| Re-entrancy deadlock | ConcurrentBag is lock-free; recursive dispatch rents new instance |
| Source returned while continuations still pending | Only return after all continuations have fired |

## Definition of Done

- [ ] All unit tests pass
- [ ] All integration tests pass
- [ ] BenchmarkDotNet shows zero Gen0 collections in steady-state dispatch
- [ ] Unity Profiler shows no GC spike during message-heavy frames
- [ ] Code review approved
- [ ] XML documentation added for pool class
