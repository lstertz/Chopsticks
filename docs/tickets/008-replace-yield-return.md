# CHOP-008: Replace yield return with Concrete Collections

## Priority: 🟠 P1 — Major Performance
## Type: Performance / GC Optimization
## Component: Native/Dependencies, Native/Messages

---

## Problem Statement

Five methods use `yield return`, each allocating a heap-resident state machine enumerator:

1. **`DependencyContainer.ResolveAll(Type)`** — allocates enumerator on every multi-resolve
2. **`DependencyContainer.GetResolutions()`** — allocates enumerator when iterating all resolutions
3. **`DependencyContainer.GetResolutions(Type)`** — allocates enumerator for typed resolution queries
4. **`IDependencyContainerExtensions.ResolveAll<T>()`** — allocates enumerator on generic multi-resolve
5. **`HandlingResult.Exceptions`** — allocates enumerator every time exceptions are accessed

Each `yield return` method compiles to a hidden class implementing `IEnumerator<T>` — this class is heap-allocated on every call, even if the enumeration is short or empty.

## Root Cause

The C# compiler generates a state machine class for each `yield return` method. These are allocated on the heap (`class`, not `struct`) and create GC pressure when called frequently.

## Solution Design

### Fix 1: `HandlingResult.Exceptions` — Return Direct Collection

The `_exceptions` field is already either null, single `Exception`, or `Exception[]`. Expose this directly:

```csharp
public readonly struct HandlingResult
{
    // Replace the yield-based property with:
    public IReadOnlyList<Exception> Exceptions
    {
        get
        {
            if (_exceptions is null)
                return Array.Empty<Exception>();
            if (_exceptions is Exception exception)
                return new[] { exception };  // Single-element array (or cache)
            return (Exception[])_exceptions;
        }
    }
}
```

Or for truly zero-alloc, use a custom struct enumerator:

```csharp
public ExceptionEnumerator GetExceptions() => new(_exceptions);

public ref struct ExceptionEnumerator
{
    private readonly object? _source;
    private int _index;
    
    public ExceptionEnumerator(object? source) { _source = source; _index = -1; }
    
    public Exception Current { get; private set; }
    
    public bool MoveNext()
    {
        if (_source is null) return false;
        if (_source is Exception ex && _index == -1) { Current = ex; _index = 0; return true; }
        if (_source is Exception[] arr && ++_index < arr.Length) { Current = arr[_index]; return true; }
        return false;
    }
}
```

### Fix 2: `DependencyContainer.ResolveAll(Type)` — Return List

```csharp
public IEnumerable<object> ResolveAll(Type contract)
{
    var resolutions = (this as IDependencyResolutionProvider).GetResolutions(contract);
    var results = new List<object>();  // Or use ArrayPool
    foreach (var resolution in resolutions)
    {
        var instance = resolution.Get(this);
        if (instance != null)
            results.Add(instance);
    }
    return results;
}
```

### Fix 3: `GetResolutions()` and `GetResolutions(Type)` — Return List

```csharp
IEnumerable<DependencyResolution> IDependencyResolutionProvider.GetResolutions()
{
    var all = new List<DependencyResolution>();
    foreach (var resolutions in _resolutions.Values)
        all.AddRange(resolutions);
    if (Parent != null)
        foreach (var resolution in Parent.GetResolutions())
            all.Add(resolution);
    return all;
}
```

### Fix 4: `IDependencyContainerExtensions.ResolveAll<T>()` — Filter In Place

```csharp
public static IEnumerable<TContract> ResolveAll<TContract>(this IDependencyContainer container)
{
    var results = new List<TContract>();
    foreach (var uncast in container.ResolveAll(typeof(TContract)))
        if (uncast is TContract typed)
            results.Add(typed);
    return results;
}
```

### Files to Modify

| File | Change |
|------|--------|
| `Native/Messages/HandlingResult.cs` | Replace yield-based Exceptions property |
| `Native/Dependencies/Containers/DependencyContainer.cs` | Replace 3 yield methods |
| `Native/Dependencies/Containers/IDependencyContainerExtensions.cs` | Replace ResolveAll<T> |

---

## Acceptance Criteria

### Functional Requirements
- [ ] All resolution methods return the same results as before
- [ ] HandlingResult.Exceptions returns correct exceptions for all cases (null, single, array)
- [ ] Empty collections are returned (not null) when there are no results
- [ ] All existing tests pass

### Performance Requirements
- [ ] No enumerator state machine allocations in resolution hot path
- [ ] HandlingResult.Exceptions access for single exception: zero allocation (if using struct enumerator)
- [ ] HandlingResult.Exceptions access for null: zero allocation

---

## Testing Specification

### Unit Tests

#### Test 1: Exceptions — Null Case
```
GIVEN HandlingResult.Success (no exceptions)
WHEN Exceptions is accessed
THEN an empty collection is returned
AND no heap allocation occurs
```

#### Test 2: Exceptions — Single Exception
```
GIVEN HandlingResult.FromException(new InvalidOperationException())
WHEN Exceptions is accessed
THEN collection contains exactly 1 exception
AND it is the correct exception
```

#### Test 3: Exceptions — Multiple Exceptions (Merged)
```
GIVEN two failure results merged together
WHEN Exceptions is accessed on the merged result
THEN collection contains both exceptions in order
```

#### Test 4: ResolveAll Returns All Implementations
```
GIVEN a container with 3 implementations of IService
WHEN ResolveAll<IService>() is called
THEN 3 instances are returned
AND they are the correct instances
```

#### Test 5: ResolveAll With Parent Inheritance
```
GIVEN a child container inheriting from parent
AND parent has 2 implementations, child has 1
WHEN child.ResolveAll<IService>() is called
THEN 3 instances are returned (1 child + 2 parent)
```

#### Test 6: GetResolutions Returns All
```
GIVEN a container with resolutions for 3 different contracts
WHEN GetResolutions() is called
THEN all resolutions across all contracts are returned
```

#### Test 7: Empty ResolveAll
```
GIVEN a container with no registrations for IService
WHEN ResolveAll<IService>() is called
THEN an empty collection is returned (not null)
```

#### Test 8: ResolveAll Type Filtering
```
GIVEN a container with IAnimal registered as Dog and Cat
WHEN ResolveAll<Dog>() is called
THEN only Dog instances are returned
```

### Performance Tests

#### Test 9: No Enumerator Allocation
```
GIVEN a container with 1 registered singleton
WHEN ResolveAll is called in a loop 10,000 times
THEN GC.GetAllocatedBytesForCurrentThread shows minimal growth
  (only the List<T> for results, not per-call state machines)
```

#### Test 10: Exceptions Property Allocation
```
GIVEN HandlingResult.Success
WHEN Exceptions is accessed 100,000 times
THEN zero allocations (Array.Empty<Exception>() is cached)
```

### Benchmarks

```
[Benchmark(Baseline = true)]
public IEnumerable<Exception> Exceptions_YieldReturn() => oldResult.Exceptions;

[Benchmark]
public IReadOnlyList<Exception> Exceptions_Direct() => newResult.Exceptions;
// Expected: Direct is 10x+ faster, 0 allocations for empty/single case
```

---

## Risks & Mitigations

| Risk | Mitigation |
|------|-----------|
| List allocation in ResolveAll | Still better than yield state machine; could pool Lists |
| Breaking change for consumers expecting lazy evaluation | ResolveAll was always fully iterated anyway |
| Struct enumerator can't be used with LINQ | Provide both: struct for perf, IEnumerable for compat |

## Definition of Done

- [ ] All yield return methods replaced
- [ ] All unit tests pass
- [ ] Benchmark shows allocation reduction
- [ ] API contract unchanged (still returns IEnumerable<T> or IReadOnlyList<T>)
- [ ] HandlingResult.Exceptions zero-alloc for empty case
