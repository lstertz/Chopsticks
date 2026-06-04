# CHOP-002: Cache RegisteredMessageHandlers Array (Copy-on-Write)

## Priority: 🔴 P0 — Critical
## Type: Performance / GC Optimization
## Component: Native/Messages/Registration

---

## Problem Statement

The `RegisteredMessageHandlers` property in `BaseMessageHandlerRegistrar` uses the C# spread operator to create a **new array on every single access**:

```csharp
// BaseMessageHandlerRegistrar.cs line 21
protected BaseRegisteredHandler<TMessage, TContext>[] RegisteredMessageHandlers => 
    [.. _registeredMessageHandlers];
```

This property is called from `BaseMulticastHandler.InitiateWithSource()` on **every message dispatch**:

```csharp
// BaseMulticastHandler.cs line 46
source.Init(RegisteredMessageHandlers);  // NEW ARRAY EVERY DISPATCH
```

At 60fps, this means 60 array allocations per second per message type — plus the element copy overhead.

## Root Cause

The spread operator `[.. list]` is syntactic sugar for `list.ToArray()` — it allocates a new array and copies all elements every time the property getter is invoked.

The TODO comment on line 20 acknowledges this:
```csharp
// TODO :: Rebuild immutable collection used during handling on any register/unregister.
```

## Solution Design

### Implementation

Convert to a cached array that is rebuilt only when handlers are registered or deregistered:

```csharp
// BaseMessageHandlerRegistrar.cs

// Replace property with cached field
protected BaseRegisteredHandler<TMessage, TContext>[] RegisteredMessageHandlers => _cachedHandlers;
private volatile BaseRegisteredHandler<TMessage, TContext>[] _cachedHandlers = [];

// Rebuild cache on mutation
private void RebuildHandlerCache()
{
    _cachedHandlers = [.. _registeredMessageHandlers];
}
```

Call `RebuildHandlerCache()` at the end of:
- `AddRegistration(BaseRegisteredHandler<TMessage, TContext> registration)` (line 133)
- `RemoveRegistration(IRegisteredHandler registration)` (line 119)

The `volatile` keyword ensures that the array reference update is visible to all threads reading `_cachedHandlers`, providing safe publication of the new snapshot.

### Files to Modify

| File | Change |
|------|--------|
| `Native/Messages/Registration/BaseMessageHandlerRegistrar.cs` | Cache array, rebuild on mutation |

---

## Acceptance Criteria

### Functional Requirements
- [ ] Registering a handler makes it available for the next dispatch
- [ ] Deregistering a handler removes it from the next dispatch
- [ ] The cached array is immutable — dispatch never sees a partially-mutated array
- [ ] All existing tests pass without modification
- [ ] Handler ordering is preserved after cache rebuild

### Performance Requirements  
- [ ] Zero allocations during message dispatch (array access is a simple field read)
- [ ] Array rebuild only occurs on register/deregister operations
- [ ] No lock contention on dispatch path (volatile read only)

---

## Testing Specification

### Unit Tests

#### Test 1: Cache Is Populated After Registration
```
GIVEN an empty BaseMessageHandlerRegistrar (via concrete subclass)
WHEN a handler is registered
THEN RegisteredMessageHandlers.Length == 1
AND RegisteredMessageHandlers[0] references the registered handler
```

#### Test 2: Cache Is Same Reference On Repeated Access
```
GIVEN a registrar with 3 handlers registered
WHEN RegisteredMessageHandlers is accessed twice
THEN both accesses return the SAME array reference (ReferenceEquals == true)
```

#### Test 3: Cache Is New Reference After Registration
```
GIVEN a registrar with 2 handlers
AND a reference to the current RegisteredMessageHandlers array
WHEN a 3rd handler is registered
THEN RegisteredMessageHandlers is a DIFFERENT reference than the cached one
AND the old array is unchanged (snapshot semantics)
```

#### Test 4: Cache Is New Reference After Deregistration
```
GIVEN a registrar with 3 handlers
AND a reference to the current RegisteredMessageHandlers array
WHEN the 2nd handler is deregistered
THEN RegisteredMessageHandlers.Length == 2
AND the old array still has Length == 3 (immutable snapshot)
```

#### Test 5: Ordering Preserved In Cache
```
GIVEN handlers registered with Order = [5, 1, 3]
THEN RegisteredMessageHandlers[0].Order == 1
AND RegisteredMessageHandlers[1].Order == 3
AND RegisteredMessageHandlers[2].Order == 5
```

#### Test 6: Dispatch During Registration Is Safe
```
GIVEN a registrar with 2 handlers
AND a dispatch is in progress (iterating the cached array)
WHEN a 3rd handler is registered concurrently
THEN the in-progress dispatch completes with the original 2 handlers
AND the next dispatch sees all 3 handlers
```

#### Test 7: Empty Cache After All Deregistered
```
GIVEN a registrar with 1 handler
WHEN that handler is deregistered
THEN RegisteredMessageHandlers is an empty array (Length == 0)
AND message dispatch returns HandlingResult.NoHandlers
```

#### Test 8: Duplicate Registration Prevention
```
GIVEN a registrar with handler A registered
WHEN handler A is registered again
THEN RegisteredMessageHandlers.Length == 1 (no duplicate)
AND cache is NOT rebuilt unnecessarily
```

### Concurrency Tests

#### Test 9: Concurrent Register + Dispatch
```
GIVEN a registrar with 5 handlers
WHEN 10 threads dispatch messages concurrently
AND 2 threads register new handlers concurrently
THEN no ArrayIndexOutOfBoundsException
AND no stale/corrupted results
AND all dispatches complete with valid HandlingResult
```

#### Test 10: Rapid Register/Deregister Cycles
```
GIVEN an empty registrar
WHEN 1000 rapid register/deregister cycles occur on one thread
AND dispatches occur on another thread
THEN no exceptions
AND final state is consistent (0 or 1 handlers depending on timing)
```

### Performance Benchmarks

#### Benchmark 1: Array Access — Before vs After
```
[Benchmark(Baseline = true)]
public BaseRegisteredHandler[] Access_SpreadOperator() => [.. _list];

[Benchmark]  
public BaseRegisteredHandler[] Access_CachedField() => _cachedArray;

// Expected: CachedField is ~100x faster (field read vs array alloc + copy)
```

#### Benchmark 2: Dispatch Allocations
```
[Benchmark]
public void Dispatch_1000Messages()
{
    for (int i = 0; i < 1000; i++)
        handler.TryHandle(message);
}
// Expected: 0 Gen0 collections (from this change alone)
```

---

## Risks & Mitigations

| Risk | Mitigation |
|------|-----------|
| Stale array visible after registration | `volatile` ensures happens-before on publish |
| Array mutated after creation | Array is created fresh from spread operator; old refs are immutable |
| Cache rebuild on every register in batch | Consider batching API or dirty flag (future optimization) |

## Definition of Done

- [ ] All unit tests pass
- [ ] All concurrency tests pass
- [ ] Benchmarks confirm zero allocation on access
- [ ] Existing test suite passes unchanged
- [ ] `volatile` keyword ensures cross-thread visibility
- [ ] TODO comment removed from source
