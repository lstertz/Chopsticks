# CHOP-014: Add IDisposable to IHandlingPromiseSource Interface

## Priority: 🟡 P2 — Moderate
## Type: Architecture / Pooling Enabler
## Component: Native/Messages/Handlers/Sources

---

## Problem Statement

`BaseHandlingPromiseSource<T>` has a `Dispose()` method that properly resets state, but `IHandlingPromiseSource` does not extend `IDisposable`. This means:
1. You cannot dispose through the interface reference
2. Pooling patterns that accept `IHandlingPromiseSource` can't call `Dispose()` for reset
3. Consumers holding interface references can't clean up resources

## Solution Design

```csharp
// IHandlingPromiseSource.cs
public interface IHandlingPromiseSource : IDisposable
{
    // ... existing members unchanged
}
```

This is a breaking change for any custom implementations that don't already implement `Dispose()`, but since this is an internal framework interface, the impact is minimal.

### Files to Modify

| File | Change |
|------|--------|
| `Native/Messages/Handlers/Sources/IHandlingPromiseSource.cs` | Extend IDisposable |
| Any custom implementations | Add Dispose() if missing |

---

## Acceptance Criteria

- [ ] `IHandlingPromiseSource` extends `IDisposable`
- [ ] All implementations have `Dispose()` method
- [ ] Pool can call `Dispose()` through interface reference
- [ ] All existing tests pass

---

## Testing Specification

### Unit Tests

#### Test 1: Dispose Through Interface
```
GIVEN an IHandlingPromiseSource reference to a SequentialHandlingPromiseSource
WHEN Dispose() is called via the interface
THEN all state is reset
AND IsCompleted == false (or appropriate default)
```

#### Test 2: All Implementations Are IDisposable
```
GIVEN all classes implementing IHandlingPromiseSource
  (via reflection scan)
THEN all implement IDisposable
AND all have a Dispose() method
```

#### Test 3: Double Dispose Safety
```
GIVEN a promise source
WHEN Dispose() is called twice
THEN no exception is thrown
```

---

## Dependencies

- **Blocks CHOP-001**: Pooling requires Dispose through interface

## Definition of Done

- [ ] Interface extended
- [ ] All implementations verified
- [ ] Tests pass
