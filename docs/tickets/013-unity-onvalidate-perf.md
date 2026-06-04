# CHOP-013: Cache OnValidate Parent Hierarchy Lookups

## Priority: 🟡 P2 — Moderate
## Type: Performance (Unity Editor)
## Component: Unity/com.chopsticks.dependencies

---

## Problem Statement

`BaseMonoContainer.OnValidate()` triggers a full parent hierarchy search on every call:

```csharp
// BaseMonoContainer.cs
public void OnValidate() => UpdateParent();
```

`UpdateParent()` calls `FindParentContainer()` which calls `GetComponentInParent<>()` — a full Transform hierarchy traversal. `OnValidate()` fires on:
- Every property change in Inspector
- Every script recompilation
- Every scene save
- Every undo/redo operation

In deep hierarchies, this causes editor hitches.

## Solution Design

```csharp
#if UNITY_EDITOR
private bool _parentDirty = true;

public void OnValidate()
{
    _parentDirty = true;
    // Defer actual lookup to next access or use EditorApplication.delayCall
    UnityEditor.EditorApplication.delayCall += () =>
    {
        if (this != null && _parentDirty)
        {
            UpdateParent();
            _parentDirty = false;
        }
    };
}
#endif
```

Or simpler — cache the result and only recompute when hierarchy changes:

```csharp
private Transform _lastParentTransform;

private void UpdateParent()
{
    if (transform.parent == _lastParentTransform && _overrideParent == null)
        return;  // No change — skip expensive lookup
    
    _lastParentTransform = transform.parent;
    // ... existing FindParentContainer logic
}
```

### Files to Modify

| File | Change |
|------|--------|
| `Unity/com.chopsticks.dependencies/Assets/Scripts/Runtime/Containers/BaseMonoContainer.cs` | Add caching/dirty flag |

---

## Acceptance Criteria

- [ ] Parent is correctly found when hierarchy changes
- [ ] Repeated OnValidate calls with no hierarchy change skip the search
- [ ] Runtime behavior unchanged (OnValidate is editor-only)
- [ ] Deep hierarchies (100+ levels) don't cause noticeable editor lag

---

## Testing Specification

### Unit Tests

#### Test 1: Parent Found on First Validate
```
GIVEN a MonoContainer as child of another MonoContainer
WHEN OnValidate fires
THEN parent is correctly identified
```

#### Test 2: Cached Result Returned on Repeat Validate
```
GIVEN parent was found on previous OnValidate
AND hierarchy has not changed
WHEN OnValidate fires again
THEN GetComponentInParent is NOT called (verify via mock/counter)
```

#### Test 3: Cache Invalidated on Reparent
```
GIVEN a cached parent
WHEN the GameObject is reparented in hierarchy
AND OnValidate fires
THEN the new parent is found correctly
```

#### Test 4: Override Parent Respected
```
GIVEN an override parent is set
WHEN OnValidate fires
THEN override parent is used (hierarchy search skipped)
```

---

## Definition of Done

- [ ] Editor performance improved for deep hierarchies
- [ ] No behavioral regression
- [ ] Tests pass in Unity Test Runner
