# CHOP-011: Replace Full Sort with Insertion-Point Binary Search

## Priority: 🟠 P1 — Major Performance
## Type: Performance
## Component: Native/Messages/Registration

---

## Problem Statement

Every handler or interceptor registration triggers a full list sort:

```csharp
// BaseMessageHandlerRegistrar.cs line 139
_registeredMessageHandlers.Add(registration);
_registeredMessageHandlers.Sort((x, y) =>
{
    int orderComparison = x.Order.CompareTo(y.Order);
    if (orderComparison != 0) return orderComparison;
    return x.RegistrationIndex.CompareTo(y.RegistrationIndex);
});
```

`List<T>.Sort()` is O(n log n) and the lambda comparison delegate may allocate a closure. Since we're inserting a single element into an already-sorted list, we can do better.

Same pattern appears in:
- `BaseRegisteredHandler.cs` line 78 (interceptor registration)
- `BaseMessageHandlerRegistrar.cs` line 69 (dispatch interceptor registration)

## Solution Design

### Binary Search for Insertion Point

```csharp
private void AddRegistration(BaseRegisteredHandler<TMessage, TContext> registration)
{
    if (_registeredMessageHandlers.Contains(registration))
        return;

    int insertIndex = FindInsertionIndex(registration);
    _registeredMessageHandlers.Insert(insertIndex, registration);
}

private int FindInsertionIndex(BaseRegisteredHandler<TMessage, TContext> item)
{
    int lo = 0, hi = _registeredMessageHandlers.Count - 1;
    while (lo <= hi)
    {
        int mid = lo + (hi - lo) / 2;
        var midItem = _registeredMessageHandlers[mid];
        
        int cmp = midItem.Order.CompareTo(item.Order);
        if (cmp == 0)
            cmp = midItem.RegistrationIndex.CompareTo(item.RegistrationIndex);
        
        if (cmp <= 0)
            lo = mid + 1;
        else
            hi = mid - 1;
    }
    return lo;
}
```

This is O(log n) for the search + O(n) for the shift from `Insert()` — but crucially:
1. No delegate allocation for the comparison
2. No full re-sort of already-ordered elements
3. Single pass through data instead of multiple merge passes

### Alternative: Use `List<T>.BinarySearch` with `IComparer<T>`

```csharp
private static readonly HandlerComparer _comparer = new();

private sealed class HandlerComparer : IComparer<BaseRegisteredHandler<TMessage, TContext>>
{
    public int Compare(BaseRegisteredHandler<TMessage, TContext>? x, 
                       BaseRegisteredHandler<TMessage, TContext>? y)
    {
        if (x == null || y == null) return 0;
        int cmp = x.Order.CompareTo(y.Order);
        return cmp != 0 ? cmp : x.RegistrationIndex.CompareTo(y.RegistrationIndex);
    }
}

private void AddRegistration(BaseRegisteredHandler<TMessage, TContext> registration)
{
    if (_registeredMessageHandlers.Contains(registration))
        return;

    int index = _registeredMessageHandlers.BinarySearch(registration, _comparer);
    if (index < 0) index = ~index;
    _registeredMessageHandlers.Insert(index, registration);
}
```

### Files to Modify

| File | Change |
|------|--------|
| `Native/Messages/Registration/BaseMessageHandlerRegistrar.cs` | Binary search insertion for handlers + interceptors |
| `Native/Messages/Registration/Handlers/BaseRegisteredHandler.cs` | Binary search insertion for per-handler interceptors |

---

## Acceptance Criteria

- [ ] Handlers are in correct order after insertion
- [ ] No full Sort() call on any registration path
- [ ] No lambda/delegate allocation for comparison
- [ ] All existing tests pass
- [ ] Insertion order preserved for equal Order values (stable)

---

## Testing Specification

### Unit Tests

#### Test 1: Ordered Insertion
```
GIVEN handlers registered with Order = [5, 1, 3, 2, 4]
THEN internal list order is [1, 2, 3, 4, 5]
```

#### Test 2: Same Order — Registration Index Tiebreak
```
GIVEN handlers A, B, C all with Order = 0
THEN order is A, B, C (registration order)
```

#### Test 3: Insert at Beginning
```
GIVEN existing handlers with Order = [2, 3, 4]
WHEN handler with Order = 1 is inserted
THEN list is [1, 2, 3, 4]
```

#### Test 4: Insert at End
```
GIVEN existing handlers with Order = [1, 2, 3]
WHEN handler with Order = 4 is inserted
THEN list is [1, 2, 3, 4]
```

#### Test 5: Insert in Middle
```
GIVEN existing handlers with Order = [1, 3, 5]
WHEN handler with Order = 3 is inserted (new registration index)
THEN list is [1, 3(old), 3(new), 5]
```

#### Test 6: Empty List Insertion
```
GIVEN empty registrar
WHEN first handler is registered
THEN list has 1 element
```

#### Test 7: Large Scale Ordering (100 handlers)
```
GIVEN 100 handlers inserted in random order (with random Order values)
THEN final list is sorted by (Order, RegistrationIndex) ascending
```

### Benchmarks

```
[Benchmark(Baseline = true)]
public void Register_WithSort() { /* Add + Sort */ }

[Benchmark]
public void Register_WithBinaryInsert() { /* BinarySearch + Insert */ }
// Expected: ~3-5x faster for n=10, less difference for n=2-3
```

---

## Definition of Done

- [ ] All Sort() calls replaced with binary insertion
- [ ] Static IComparer instances (no delegate allocation)
- [ ] Ordering tests pass
- [ ] No regression in existing tests
