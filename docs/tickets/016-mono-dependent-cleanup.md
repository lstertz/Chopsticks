# CHOP-016: Add OnDestroy Cleanup to BaseMonoDependent

## Priority: 🟡 P2 — Moderate
## Type: Memory Leak Prevention
## Component: Unity/com.chopsticks.dependencies

---

## Problem Statement

`BaseMonoDependency` and `BaseMonoDependencyWrapper` properly deregister in `OnDisable()`, but `BaseMonoDependent` (the consumer side) holds a reference to the container without clearing it on destroy:

```csharp
public abstract class BaseMonoDependent<...> : MonoBehaviour, IUnityDependent<...>
{
    public TNativeContainer Container { get; set; }
    // NO OnDestroy!
}
```

When a `BaseMonoDependent` GameObject is destroyed, its `Container` reference keeps the native container alive if nothing else references it — preventing GC collection.

## Solution Design

```csharp
protected virtual void OnDestroy()
{
    Container = default;  // Clear reference for GC
}
```

### Files to Modify

| File | Change |
|------|--------|
| `Unity/com.chopsticks.dependencies/Assets/Scripts/Runtime/BaseMonoDependent.cs` | Add OnDestroy |

---

## Acceptance Criteria

- [ ] Container reference cleared on destroy
- [ ] No NullReferenceException if dependent is accessed after destroy
- [ ] All existing tests pass

---

## Testing Specification

### Unit Tests

#### Test 1: Container Cleared on Destroy
```
GIVEN a BaseMonoDependent with Container set
WHEN OnDestroy is called
THEN Container is null/default
```

#### Test 2: Weak Reference Collection
```
GIVEN a BaseMonoDependent holding a container
AND a WeakReference to that container
WHEN the dependent is destroyed
AND GC.Collect() is called
THEN the WeakReference.IsAlive == false (container is collected)
  (assuming no other strong references)
```

#### Test 3: Virtual Override Not Required
```
GIVEN a subclass of BaseMonoDependent that doesn't override OnDestroy
WHEN the GameObject is destroyed
THEN base OnDestroy still fires (cleanup happens)
```

---

## Definition of Done

- [ ] OnDestroy added as virtual method
- [ ] Container reference cleared
- [ ] No regression in existing behavior
