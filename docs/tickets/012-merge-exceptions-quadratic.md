# CHOP-012: Fix O(N²) Allocation in MergeExceptions

## Priority: 🟠 P1 — Major Performance
## Type: Performance / GC Optimization
## Component: Native/Messages

---

## Problem Statement

When merging multiple failure results sequentially, `MergeExceptions` allocates a new array for each merge:

```csharp
// HandlingResult.cs
private Exception[] MergeExceptions(Exception[] exceptions, Exception exception)
{
    var merged = new Exception[exceptions.Length + 1];
    Array.Copy(exceptions, merged, exceptions.Length);
    merged[^1] = exception;
    return merged;
}
```

If N failures are merged sequentially:
- Merge 1: allocate array[2], copy 1 element
- Merge 2: allocate array[3], copy 2 elements  
- Merge 3: allocate array[4], copy 3 elements
- ...
- Merge N: allocate array[N+1], copy N elements

**Total allocation: O(N²) bytes. Total copy operations: O(N²).**

## Solution Design

### Option A: Use List<Exception> Internally

Change `_exceptions` from `object?` to a more structured type:

```csharp
private readonly object? _exceptions;  // Keep polymorphic storage for 0/1 case

public HandlingResult MergeWith(HandlingResult other)
{
    // For the merge case, collect into a list then convert once
    if (/* both are failures */)
    {
        var allExceptions = new List<Exception>();
        foreach (var ex in this.Exceptions) allExceptions.Add(ex);
        foreach (var ex in other.Exceptions) allExceptions.Add(ex);
        return new HandlingResult(allExceptions.ToArray());
    }
}
```

### Option B: ArraySegment with Pooled Arrays (Zero-Copy for Common Case)

```csharp
private Exception[] MergeExceptions(Exception[] exceptionsA, Exception[] exceptionsB)
{
    var merged = new Exception[exceptionsA.Length + exceptionsB.Length];
    exceptionsA.CopyTo(merged.AsSpan());
    exceptionsB.CopyTo(merged.AsSpan(exceptionsA.Length));
    return merged;
}
```

This is already what the code does for the array+array case, but the single-to-array case could avoid intermediate allocations.

### Option C: Store Exceptions as ImmutableArray<Exception>.Builder (Best for Iterative Merging)

For the sequential merge pattern in `SequentialHandlingPromiseSource`:

```csharp
// In the sequential handler loop:
private List<Exception>? _accumulatedExceptions;

private void OnHandlerCompletion()
{
    var current = _currentAwaiter.GetResult();
    if (current.Status == HandlingStatus.Failure)
    {
        _accumulatedExceptions ??= new List<Exception>();
        foreach (var ex in current.Exceptions)
            _accumulatedExceptions.Add(ex);
    }
    _currentIndex++;
    Step();
}
```

Then build the final `HandlingResult` once at completion instead of merging N times.

### Files to Modify

| File | Change |
|------|--------|
| `Native/Messages/HandlingResult.cs` | Optimize MergeExceptions |
| `Native/Messages/Handlers/Sources/SequentialHandlingPromiseSource.cs` | Accumulate exceptions, build result once |

---

## Acceptance Criteria

- [ ] Merging N failures allocates O(N) total, not O(N²)
- [ ] Final HandlingResult contains all exceptions in correct order
- [ ] Single-exception case still has zero extra allocation
- [ ] All existing tests pass

---

## Testing Specification

### Unit Tests

#### Test 1: Merge Two Single Exceptions
```
GIVEN resultA with exception A and resultB with exception B
WHEN MergeWith is called
THEN result has exceptions [A, B] in order
```

#### Test 2: Merge N Failures Sequentially
```
GIVEN 10 failure results each with 1 exception
WHEN they are merged sequentially (A.Merge(B).Merge(C)...)
THEN final result has all 10 exceptions
AND total array allocations <= 2 (not 9)
```

#### Test 3: Merge Array + Single
```
GIVEN resultA with exceptions [A1, A2, A3] and resultB with exception B
WHEN MergeWith is called
THEN result has exceptions [A1, A2, A3, B]
```

#### Test 4: Merge Array + Array
```
GIVEN resultA with [A1, A2] and resultB with [B1, B2, B3]
WHEN MergeWith is called
THEN result has [A1, A2, B1, B2, B3]
```

#### Test 5: Merge With Non-Failure (Success)
```
GIVEN a failure result and a success result
WHEN MergeWith is called
THEN the failure is preserved (success doesn't clear exceptions)
```

#### Test 6: Accumulation in Sequential Handler
```
GIVEN 5 handlers, 3 of which throw
WHEN sequential dispatch completes
THEN the final result has exactly 3 exceptions
AND they're in handler execution order
```

### Allocation Benchmarks

```
[Benchmark]
[Arguments(2, 5, 10, 50)]
public HandlingResult MergeN_Failures(int count)
{
    var result = HandlingResult.NoHandlers;
    for (int i = 0; i < count; i++)
        result = result.MergeWith(HandlingResult.FromException(new Exception()));
    return result;
}
// Expected: After fix, allocation count is O(1) not O(N)
```

---

## Definition of Done

- [ ] O(N²) merge pattern eliminated
- [ ] All tests pass
- [ ] Benchmark confirms linear allocation scaling
- [ ] Exception ordering preserved
