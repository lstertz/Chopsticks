# CHOP-004: Thread-Safe ContainedResolution.Get()

## Priority: 🔴 P0 — Critical
## Type: Bug / Thread Safety
## Component: Native/Dependencies/Resolutions

---

## Problem Statement

`ContainedResolution.Get()` uses a `Dictionary<IDependencyContainer, object>` without any synchronization:

```csharp
// ContainedResolution.cs
private readonly Dictionary<IDependencyContainer, object> _instances = new(1);

public override object? Get(IDependencyContainer container)
{
    if (!_instances.TryGetValue(container, out var instance))
    {
        instance = Factory?.Invoke(container);
        if (instance is not null)
            _instances.Add(container, instance);  // NOT THREAD-SAFE
    }
    return instance;
}
```

`Dictionary<K,V>` is **not thread-safe for concurrent read/write operations**. Two threads calling `Get()` with different container keys can corrupt the dictionary's internal hash table — causing infinite loops in bucket chains, lost entries, or `IndexOutOfRangeException`.

Even two threads calling `Get()` with the **same** container can race: both see `TryGetValue` return false, both invoke the factory, and `Add()` throws `ArgumentException` ("key already exists").

## Root Cause

**File:** `Native/Dependencies/Resolutions/ContainedResolution.cs`

`Dictionary<K,V>` has no internal locking. Concurrent `Add()` calls can corrupt internal state. `TryGetValue` during a concurrent resize can return incorrect results or throw.

## Solution Design

### Implementation: ConcurrentDictionary with GetOrAdd

```csharp
using System.Collections.Concurrent;

public class ContainedResolution(Type contract,
    Func<IDependencyContainer, object?> factory) :
    DependencyResolution(contract, factory)
{
    private readonly ConcurrentDictionary<IDependencyContainer, object> _instances = new();

    public override void Dispose()
    {
        base.Dispose();
        foreach (var instance in _instances.Values)
            if (instance is IDisposable disposable)
                disposable.Dispose();
        _instances.Clear();
    }

    public override void DisposeFor(IDependencyContainer container)
    {
        if (_instances.TryRemove(container, out var instance))
            if (instance is IDisposable disposable)
                disposable.Dispose();
    }

    public override object? Get(IDependencyContainer container)
    {
        if (Factory == null)
            return null;

        return _instances.GetOrAdd(container, c => Factory!.Invoke(c)!);
    }
}
```

### Important Design Decision: Factory Invocation Count

`ConcurrentDictionary.GetOrAdd` may invoke the factory multiple times under contention (the factory is not guaranteed to run exactly once). Only one result is stored, but the factory may execute multiple times.

If exactly-once factory execution is required:
```csharp
private readonly ConcurrentDictionary<IDependencyContainer, Lazy<object>> _instances = new();

public override object? Get(IDependencyContainer container)
{
    if (Factory == null) return null;
    
    var lazy = _instances.GetOrAdd(container, 
        c => new Lazy<object>(() => Factory!.Invoke(c)!));
    return lazy.Value;
}
```

### Files to Modify

| File | Change |
|------|--------|
| `Native/Dependencies/Resolutions/ContainedResolution.cs` | Replace Dictionary with ConcurrentDictionary |

---

## Acceptance Criteria

### Functional Requirements
- [ ] Each container gets its own instance (contained scope semantics preserved)
- [ ] Same container always gets same instance
- [ ] Different containers get different instances
- [ ] Dispose() properly disposes all instances
- [ ] DisposeFor() only disposes the specified container's instance
- [ ] Factory invoked at most once per container (if using Lazy approach)
- [ ] All existing dependency tests pass

### Performance Requirements
- [ ] No lock contention for reads of already-created instances
- [ ] Concurrent access from different containers doesn't block
- [ ] Memory overhead of ConcurrentDictionary is acceptable (uses striped locks)

---

## Testing Specification

### Unit Tests

#### Test 1: Per-Container Instance Isolation
```
GIVEN a ContainedResolution
AND containerA and containerB
WHEN Get(containerA) and Get(containerB) are called
THEN different instances are returned for each container
AND Get(containerA) always returns the same instance
```

#### Test 2: Thread Safety — Same Container Concurrent Access
```
GIVEN a ContainedResolution with a factory that takes 5ms
WHEN 20 threads call Get(sameContainer) concurrently
THEN all threads receive the same instance
AND the factory is invoked at most once (with Lazy approach)
```

#### Test 3: Thread Safety — Different Containers Concurrent Access
```
GIVEN a ContainedResolution
AND 20 different containers
WHEN 20 threads each call Get() with their own container concurrently
THEN each thread gets a unique instance for its container
AND no exception is thrown
AND no dictionary corruption occurs
```

#### Test 4: Dispose All Instances
```
GIVEN a ContainedResolution with instances for 5 containers
AND all instances implement IDisposable
WHEN Dispose() is called
THEN all 5 instances have Dispose() called
AND the internal dictionary is cleared
AND subsequent Get() calls return null
```

#### Test 5: DisposeFor Single Container
```
GIVEN a ContainedResolution with instances for containers A, B, C
WHEN DisposeFor(containerB) is called
THEN instance B is disposed
AND Get(containerA) still returns instance A
AND Get(containerC) still returns instance C
AND Get(containerB) creates a new instance (if factory still available)
```

#### Test 6: Concurrent Dispose Safety
```
GIVEN a ContainedResolution with instances
WHEN Dispose() is called concurrently with Get() from other threads
THEN no exception is thrown
AND no double-dispose of any instance
```

#### Test 7: Factory Returns Null
```
GIVEN a ContainedResolution with a factory that returns null
WHEN Get(container) is called
THEN null is returned
AND no entry is stored in the dictionary (or a null-sentinel is stored)
AND calling Get(container) again invokes the factory again
```

#### Test 8: Dictionary Corruption Regression
```
GIVEN a ContainedResolution
WHEN 100 threads perform mixed Get/DisposeFor operations for 3 seconds
THEN no IndexOutOfRangeException
AND no infinite loops (timeout after 5 seconds)
AND no KeyNotFoundException
```

### Stress Tests

#### Test 9: High Container Churn
```
GIVEN a ContainedResolution
WHEN containers are rapidly created, resolved, and disposed for
  (1000 cycles)
THEN memory does not leak (instances are properly disposed)
AND the dictionary count reflects only active containers
```

#### Test 10: Memory Pressure Under Many Containers
```
GIVEN a ContainedResolution
WHEN 10,000 unique containers each resolve
THEN ConcurrentDictionary handles this without excessive memory
AND all instances are accessible
```

### Performance Benchmarks

#### Benchmark 1: Read Performance (Already Created)
```
[Benchmark]
public object Get_ExistingInstance() => resolution.Get(container);
// Expected: < 100ns (ConcurrentDictionary read is lock-free for existing keys)
```

#### Benchmark 2: Concurrent Read Throughput
```
[Benchmark]
[Arguments(1, 4, 8, 16)]
public void ConcurrentReads(int threads)
// Expected: Near-linear scaling (no lock contention on reads)
```

---

## Risks & Mitigations

| Risk | Mitigation |
|------|-----------|
| ConcurrentDictionary overhead vs Dictionary | Only matters for single-threaded; reads are lock-free |
| GetOrAdd invokes factory multiple times | Use Lazy<object> wrapper for exactly-once semantics |
| Null factory after Dispose | Check Factory == null before GetOrAdd |
| DisposeFor during GetOrAdd race | TryRemove is atomic; at worst factory runs and result is immediately disposed |

## Definition of Done

- [ ] All unit tests pass
- [ ] Stress tests pass under ThreadSanitizer or stress conditions
- [ ] No dictionary corruption under concurrent access
- [ ] Exactly one instance per container under contention (with Lazy approach)
- [ ] Benchmark confirms read performance is acceptable
- [ ] Existing tests pass unchanged
