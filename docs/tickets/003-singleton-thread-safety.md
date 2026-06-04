# CHOP-003: Thread-Safe SingletonResolution.Get()

## Priority: 🔴 P0 — Critical
## Type: Bug / Thread Safety
## Component: Native/Dependencies/Resolutions

---

## Problem Statement

`SingletonResolution.Get()` uses the null-coalescing assignment operator (`??=`) which is **NOT atomic**:

```csharp
// SingletonResolution.cs line 29
public override object? Get(IDependencyContainer container) =>
    _instance ??= Factory?.Invoke(container);
```

Under concurrent access, two threads can both observe `_instance` as null, both invoke the factory, and you end up with **two instances of a "singleton."** The second assignment overwrites the first — the first instance leaks without disposal.

This is a classic double-checked locking bug. The `??=` operator compiles to:
```csharp
if (_instance == null)
    _instance = Factory?.Invoke(container);  // RACE: both threads enter here
return _instance;
```

## Root Cause

**File:** `Native/Dependencies/Resolutions/SingletonResolution.cs`

The `??=` operator provides no atomicity guarantees. In a multi-threaded environment (Unity's job system, async/await continuations, background threads), this is a data race.

## Solution Design

### Option A: Lazy<T> Pattern (Recommended)

```csharp
public class SingletonResolution(Type contract,
    Func<IDependencyContainer, object?> factory) :
    DependencyResolution(contract, factory)
{
    private object? _instance;
    private readonly object _lock = new();
    private bool _isCreated;

    public override void Dispose()
    {
        base.Dispose();
        lock (_lock)
        {
            if (_instance is IDisposable disposable)
                disposable.Dispose();
            _instance = null;
            _isCreated = false;
        }
    }

    public override object? Get(IDependencyContainer container)
    {
        if (_isCreated)
            return _instance;  // Fast path: volatile read after init

        lock (_lock)
        {
            if (_isCreated)
                return _instance;  // Double-check inside lock
            
            _instance = Factory?.Invoke(container);
            _isCreated = true;
            return _instance;
        }
    }
}
```

### Option B: LazyInitializer.EnsureInitialized (Simpler)

```csharp
public override object? Get(IDependencyContainer container)
{
    return LazyInitializer.EnsureInitialized(
        ref _instance, 
        ref _isCreated, 
        ref _lock,
        () => Factory?.Invoke(container));
}
```

### Why Not `Lazy<T>`?

`Lazy<T>` can't be used here because the factory requires the `container` parameter, which isn't available at construction time.

### Files to Modify

| File | Change |
|------|--------|
| `Native/Dependencies/Resolutions/SingletonResolution.cs` | Add lock + double-check pattern |

---

## Acceptance Criteria

### Functional Requirements
- [ ] Singleton dependencies are created exactly once regardless of concurrent access
- [ ] The factory is invoked at most once
- [ ] Get() returns the same instance on every call
- [ ] Dispose() properly cleans up the singleton instance
- [ ] After Dispose(), Get() returns null (factory is null)
- [ ] All existing dependency tests pass

### Performance Requirements
- [ ] Fast path (after creation) requires no lock acquisition
- [ ] Lock is only held during initial creation
- [ ] No performance regression for single-threaded access patterns

---

## Testing Specification

### Unit Tests

#### Test 1: Single Instance Created
```
GIVEN a SingletonResolution with a factory that increments a counter
WHEN Get() is called 100 times
THEN the factory counter == 1
AND all 100 calls return the same reference
```

#### Test 2: Thread Safety — Single Instance Under Contention
```
GIVEN a SingletonResolution with a factory that takes 10ms (Thread.Sleep)
WHEN 50 threads call Get() concurrently
THEN the factory is invoked exactly once
AND all 50 threads receive the same instance reference
```

#### Test 3: Thread Safety — No Leaked Instance
```
GIVEN a SingletonResolution with a factory that creates IDisposable objects
AND a counter tracking created instances
WHEN 20 threads call Get() concurrently
THEN exactly 1 instance is created (counter == 1)
AND Dispose() is called exactly 1 time on that instance
```

#### Test 4: Fast Path Performance
```
GIVEN a SingletonResolution that has already been resolved once
WHEN Get() is called 1,000,000 times in a tight loop
THEN execution time is < 50ms (no lock overhead on fast path)
```

#### Test 5: Null Factory After Dispose
```
GIVEN a SingletonResolution that has been resolved
WHEN Dispose() is called
AND Get() is called again
THEN Get() returns null
AND no exception is thrown
```

#### Test 6: Dispose Thread Safety
```
GIVEN a SingletonResolution
WHEN Get() and Dispose() are called concurrently from different threads
THEN no exception is thrown
AND the instance is either fully created and disposed, or never created
```

#### Test 7: Factory Exception Handling
```
GIVEN a SingletonResolution with a factory that throws on first call
WHEN Get() is called and catches the exception
AND Get() is called again
THEN the factory is invoked again (no cached null from failed creation)
OR the original exception is re-thrown (design decision)
```

#### Test 8: Memory Barrier Correctness
```
GIVEN a SingletonResolution
WHEN thread A calls Get() (creating the instance)
AND thread B calls Get() 1ns later
THEN thread B sees the fully constructed object (not a partially constructed reference)
AND all fields of the returned object are properly initialized
```

### Stress Tests

#### Test 9: High-Contention Stress
```
GIVEN 100 different SingletonResolutions
WHEN 200 threads randomly resolve from these resolutions for 5 seconds
THEN each resolution creates exactly 1 instance
AND no deadlocks occur
AND no exceptions escape
```

#### Test 10: Create + Dispose + Recreate Cycle
```
GIVEN a container that registers, resolves, deregisters, and re-registers singletons
WHEN this cycle is repeated 1000 times concurrently
THEN no memory leaks (instances are properly disposed)
AND no duplicate instances in circulation
```

---

## Risks & Mitigations

| Risk | Mitigation |
|------|-----------|
| Lock object adds 24 bytes per resolution | Acceptable — singletons are few, created once |
| Deadlock if factory resolves other singletons | Factories should not hold locks; document this contract |
| Performance regression in single-threaded Unity | Fast path (bool check + field read) has negligible overhead |

## Definition of Done

- [ ] All unit tests pass (including thread safety tests)
- [ ] Stress tests pass with no races detected (run with ThreadSanitizer if available)
- [ ] BenchmarkDotNet confirms fast path has < 1ns overhead vs previous implementation
- [ ] Factory invocation count verified to be exactly 1 under all concurrent scenarios
- [ ] Dispose properly cleans up singleton instances
