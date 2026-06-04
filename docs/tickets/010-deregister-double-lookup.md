# CHOP-010: Fix Double Dictionary Lookup in Deregister

## Priority: 🟠 P1 — Major Performance
## Type: Performance
## Component: Native/Dependencies/Containers

---

## Problem Statement

`DependencyContainer.Deregister()` performs two dictionary lookups for the same key:

```csharp
// DependencyContainer.cs lines 67-72
public IDependencyContainer Deregister(DependencyRegistration registration)
{
    if (!_resolutions.ContainsKey(registration.Contract))  // Lookup 1
        return this;

    int index = 0;
    DependencyResolution? resolution = null;
    var resolutions = _resolutions[registration.Contract];  // Lookup 2 (SAME KEY)
    // ...
}
```

Each dictionary lookup is O(1) amortized but involves hash computation and potential bucket traversal. Doing it twice is wasteful.

## Solution Design

```csharp
public IDependencyContainer Deregister(DependencyRegistration registration)
{
    if (!_resolutions.TryGetValue(registration.Contract, out var resolutions))
        return this;  // Single lookup: checks existence AND retrieves value

    int index = 0;
    DependencyResolution? resolution = null;
    int count = resolutions.Count;
    for (; index < count; index++)
    {
        if (ReferenceEquals(resolutions[index].Registration, registration))
        {
            resolution = resolutions[index];
            break;
        }
    }

    if (resolution == null)
        return this;

    resolution.Dispose();
    resolutions.RemoveAt(index);
    if (resolutions.Count == 0)
        _resolutions.Remove(registration.Contract);

    return this;
}
```

### Files to Modify

| File | Change |
|------|--------|
| `Native/Dependencies/Containers/DependencyContainer.cs` | Replace ContainsKey + indexer with TryGetValue |

---

## Acceptance Criteria

- [ ] Single dictionary lookup per Deregister call
- [ ] All existing tests pass
- [ ] Behavior is identical (same return values, same disposal)

---

## Testing Specification

### Unit Tests

#### Test 1: Deregister Existing Registration
```
GIVEN a container with registered dependency X
WHEN Deregister(registrationX) is called
THEN the dependency is removed
AND subsequent Resolve<X> returns false
```

#### Test 2: Deregister Non-Existent Contract
```
GIVEN a container with NO registration for type Y
WHEN Deregister(registrationY) is called
THEN the container is returned unchanged
AND no exception is thrown
```

#### Test 3: Deregister Non-Existent Registration for Existing Contract
```
GIVEN a container with registration A for contract X
WHEN Deregister(registrationB) is called (different registration, same contract)
THEN registration A remains
AND container is returned unchanged
```

#### Test 4: Deregister Last Registration Removes Key
```
GIVEN a container with exactly 1 registration for contract X
WHEN Deregister(that registration) is called
THEN the contract key is removed from internal dictionary
AND CanProvide(typeof(X)) returns false
```

#### Test 5: Deregister One of Multiple Registrations
```
GIVEN a container with 3 registrations for contract X
WHEN the 2nd registration is deregistered
THEN ResolveAll<X>() returns 2 instances
AND the deregistered instance is not among them
```

### Benchmarks

```
[Benchmark]
public void Deregister_SingleLookup() => container.Deregister(reg);
// Expected: ~30% faster than double-lookup version for hash-expensive keys
```

---

## Definition of Done

- [ ] TryGetValue used instead of ContainsKey + indexer
- [ ] All tests pass
- [ ] Benchmark confirms improvement
