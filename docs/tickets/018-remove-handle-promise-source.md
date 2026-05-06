# CHOP-018: Remove Redundant HandlePromiseSource Wrapper

## Priority: 🟢 P3 — Minor
## Type: Code Cleanup
## Component: Native/Messages/Handlers/Sources

---

## Problem Statement

`HandlePromiseSource` wraps `IHandlingPromiseSource` and does nothing except delegate:

```csharp
public class HandlePromiseSource : BaseHandlingPromiseSource<IHandlingPromiseSource>
{
    public override bool IsCompleted => InnerSource!.IsCompleted;

    public override HandlingResult GetResult()
    {
        var result = InnerSource!.GetResult();
        return result;  // Just returns it — no transformation
    }

    public override void OnCompleted(Action continuation) =>
        InnerSource!.OnCompleted(continuation);
}
```

Compare with `HandleAsyncPromiseSource` which actually transforms the result (throws on failure/cancellation). `HandlePromiseSource` adds zero value — it's pure indirection and an unnecessary heap allocation.

## Solution Design

Remove `HandlePromiseSource` and use the inner source directly wherever this class is currently instantiated.

### Files to Modify

| File | Change |
|------|--------|
| `Native/Messages/Handlers/Sources/HandlePromiseSource.cs` | **DELETE** |
| Any files that instantiate HandlePromiseSource | Use inner source directly |

---

## Acceptance Criteria

- [ ] HandlePromiseSource.cs deleted
- [ ] All usages replaced with direct inner source
- [ ] No behavioral change
- [ ] All tests pass

---

## Testing Specification

### Unit Tests

#### Test 1: Replacement Produces Same Results
```
GIVEN the code path that previously used HandlePromiseSource
WHEN a message is dispatched through that path
THEN the same HandlingResult is produced as before
```

#### Test 2: Compilation Succeeds Without File
```
GIVEN HandlePromiseSource.cs is deleted
WHEN the project is compiled
THEN no compilation errors
```

---

## Definition of Done

- [ ] File deleted
- [ ] No references remain
- [ ] Tests pass
