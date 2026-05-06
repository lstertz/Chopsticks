# CHOP-017: Convert HandlingResult Static Properties to Static Readonly Fields

## Priority: 🟡 P2 — Moderate
## Type: Performance / Correctness
## Component: Native/Messages

---

## Problem Statement

`HandlingResult` uses static **properties** (evaluated on every access) instead of static **fields** (initialized once):

```csharp
public static HandlingResult Cancelled => new() { Status = HandlingStatus.Cancelled };
public static HandlingResult NoHandlers => new() { Status = HandlingStatus.NotHandled };
public static HandlingResult Success => new() { Status = HandlingStatus.Success };
```

Each access evaluates `new()` and initializes the struct. While `HandlingResult` is a readonly struct (so copies are cheap), the property getter still involves function call overhead and struct initialization on every access.

## Solution Design

```csharp
public static readonly HandlingResult Cancelled = new() { Status = HandlingStatus.Cancelled };
public static readonly HandlingResult NoHandlers = new() { Status = HandlingStatus.NotHandled };
public static readonly HandlingResult Success = new() { Status = HandlingStatus.Success };
```

**Note:** Since `HandlingResult` is a `readonly struct`, the fields return copies regardless — but the JIT can inline static readonly field access much more aggressively than property access.

### Files to Modify

| File | Change |
|------|--------|
| `Native/Messages/HandlingResult.cs` | Change `=>` properties to `=` fields |

---

## Acceptance Criteria

- [ ] Static members are fields, not properties
- [ ] Values are identical to previous (Cancelled, NoHandlers, Success)
- [ ] All existing tests pass
- [ ] JIT can inline field reads

---

## Testing Specification

### Unit Tests

#### Test 1: Cancelled Has Correct Status
```
THEN HandlingResult.Cancelled.Status == HandlingStatus.Cancelled
AND HandlingResult.Cancelled._exceptions == null
```

#### Test 2: NoHandlers Has Correct Status
```
THEN HandlingResult.NoHandlers.Status == HandlingStatus.NotHandled
```

#### Test 3: Success Has Correct Status
```
THEN HandlingResult.Success.Status == HandlingStatus.Success
```

#### Test 4: Reference Stability (struct copy semantics)
```
GIVEN var a = HandlingResult.Success
AND var b = HandlingResult.Success
THEN a.Status == b.Status (both are copies with same values)
```

---

## Definition of Done

- [ ] Properties converted to static readonly fields
- [ ] All tests pass
- [ ] No behavioral change
