# CHOP-006: Fix Shared Mutable State in HandlingResultPromise Static Sources

## Priority: 🔴 P0 — Critical
## Type: Bug / Race Condition
## Component: Native/Messages

---

## Problem Statement

`HandlingResultPromise` has static fields that share `IHandlingPromiseSource` instances across all callers:

```csharp
// HandlingResultPromise.cs lines 11-18
public static HandlingResultPromise NoHandlers => new(_noHandlersSource);
private static readonly IHandlingPromiseSource _noHandlersSource =
    new TryHandlePromiseSource().Init(HandlingResult.NoHandlers);

public static HandlingResultPromise Success => new(_successSource);
private static readonly IHandlingPromiseSource _successSource =
    new TryHandlePromiseSource().Init(HandlingResult.Success);
```

`IHandlingPromiseSource` has **mutable properties**:
```csharp
Action? OnCancelled { get; set; }
Action<HandlingResult>? OnCompletion { get; set; }
Action<IEnumerable<Exception>>? OnFailure { get; set; }
Action<HandlingResult>? OnNonSuccess { get; set; }
Action? OnSuccess { get; set; }
SynchronizationContext? FailureContext { get; set; }
```

When two callers both use `HandlingResultPromise.NoHandlers` and set callbacks:
```csharp
// Thread A
var promise = HandlingResultPromise.NoHandlers;
promise.OnCompletion(result => HandleA(result));  // Sets _noHandlersSource.OnCompletion

// Thread B  
var promise = HandlingResultPromise.NoHandlers;
promise.OnCompletion(result => HandleB(result));  // OVERWRITES Thread A's callback!
```

Thread A's callback is silently lost.

## Root Cause

**File:** `Native/Messages/HandlingResultPromise.cs`

The static sources are singletons shared across all promise instances. Since `HandlingResultPromise` is a struct wrapping a reference to the source, multiple promise structs point to the **same underlying source object**.

## Solution Design

### Option A: Create Fresh Source Per Access (Safe, Simple)

```csharp
public static HandlingResultPromise NoHandlers => 
    new(new TryHandlePromiseSource().Init(HandlingResult.NoHandlers));

public static HandlingResultPromise Success => 
    new(new TryHandlePromiseSource().Init(HandlingResult.Success));
```

This allocates per access but `TryHandlePromiseSource` is tiny (it's already completed synchronously).

### Option B: Immutable Source for Static Cases (Zero-Alloc, Correct)

Create a sealed source implementation that ignores callback mutations:

```csharp
// New file: Native/Messages/Handlers/Sources/ImmutablePromiseSource.cs
internal sealed class ImmutablePromiseSource : IHandlingPromiseSource
{
    private readonly HandlingResult _result;
    
    public ImmutablePromiseSource(HandlingResult result) => _result = result;

    public bool IsCompleted => true;
    public HandlingResult GetResult() => _result;
    public void OnCompleted(Action continuation) => continuation();  // Already complete

    // All callbacks are no-ops — this source is immutable
    public Action InitiateDefaultContinuations => () => { };
    public Action? OnCancelled { get => null; set { } }
    public Action<HandlingResult>? OnCompletion { get => null; set { } }
    public Action<IEnumerable<Exception>>? OnFailure { get => null; set { } }
    public Action<HandlingResult>? OnNonSuccess { get => null; set { } }
    public Action? OnSuccess { get => null; set { } }
    public SynchronizationContext? FailureContext { get => null; set { } }
}
```

Then the statics become:
```csharp
private static readonly IHandlingPromiseSource _noHandlersSource =
    new ImmutablePromiseSource(HandlingResult.NoHandlers);
private static readonly IHandlingPromiseSource _successSource =
    new ImmutablePromiseSource(HandlingResult.Success);
```

### Option C: Hybrid — Check Completion Before Allowing Callbacks

In the `HandlingResultPromise` methods that set callbacks, check if already completed and invoke immediately instead of storing:

```csharp
public HandlingResultPromise OnCompletion(Action<HandlingResult> onCompletion)
{
    if (_source.IsCompleted)
    {
        var result = _source.GetResult();
        if ((result.Status & HandlingStatus.Completed) != 0)
            onCompletion(result);
        return this;  // Don't store on shared source
    }
    _source.OnCompletion = onCompletion;
    return this;
}
```

**Wait — this is already what the code does!** Looking at lines 51-64:
```csharp
public HandlingResultPromise OnCompletion(Action<HandlingResult> onCompletion)
{
    if (!_source.IsCompleted)
    {
        _source.OnCompletion = onCompletion;  // Only stored if NOT completed
        return this;
    }
    var result = _source.GetResult();
    if ((result.Status & HandlingStatus.Completed) != 0)
        onCompletion(result);
    return this;
}
```

So the actual bug is **only in the constructor path** (`HandlingResultPromise(IHandlingPromiseSource source)`):
```csharp
public HandlingResultPromise(IHandlingPromiseSource source)
{
    _source = source;
    if (!_source.IsCompleted)
        _source.OnCompleted(_source.InitiateDefaultContinuations);  // Sets on shared source!
}
```

But for the static cases, `_source.IsCompleted` is always `true`, so this branch is never taken. **The real risk is if someone chains `.ThrowIfFailed()` or sets `FailureContext`.**

### Recommended: Option B (ImmutablePromiseSource)

Safest approach — the source silently ignores all mutation attempts, and the promise methods handle the "already completed" case inline.

### Files to Modify

| File | Change |
|------|--------|
| `Native/Messages/Handlers/Sources/ImmutablePromiseSource.cs` | **NEW** — Immutable source |
| `Native/Messages/HandlingResultPromise.cs` | Use ImmutablePromiseSource for statics |

---

## Acceptance Criteria

### Functional Requirements
- [ ] `HandlingResultPromise.NoHandlers` returns correct status
- [ ] `HandlingResultPromise.Success` returns correct status  
- [ ] Callbacks set on static promises are invoked immediately (not stored)
- [ ] Two callers using the same static promise don't interfere
- [ ] All existing message handling tests pass

### Performance Requirements
- [ ] Static promise access requires zero allocation (ImmutablePromiseSource is singleton)
- [ ] No performance regression for dynamic (non-static) promises

---

## Testing Specification

### Unit Tests

#### Test 1: Static NoHandlers Correctness
```
GIVEN HandlingResultPromise.NoHandlers
THEN Status == HandlingStatus.NotHandled
AND Source.IsCompleted == true
```

#### Test 2: Static Success Correctness
```
GIVEN HandlingResultPromise.Success
THEN Status == HandlingStatus.Success
AND Source.IsCompleted == true
```

#### Test 3: Concurrent Callback Independence
```
GIVEN HandlingResultPromise.Success accessed by 100 threads
WHEN each thread sets OnCompletion with a unique callback
THEN each callback is invoked exactly once (inline, not stored)
AND no callback is lost or overwritten
```

#### Test 4: ImmutablePromiseSource Ignores Mutations
```
GIVEN an ImmutablePromiseSource
WHEN OnCancelled is set to a non-null value
THEN OnCancelled getter returns null
AND no exception is thrown
```

#### Test 5: ImmutablePromiseSource Completes Immediately
```
GIVEN an ImmutablePromiseSource
WHEN OnCompleted(callback) is called
THEN callback is invoked immediately (synchronously)
```

#### Test 6: Dynamic Promise Still Works
```
GIVEN a HandlingResultPromise wrapping a real SequentialHandlingPromiseSource
WHEN callbacks are set before completion
THEN callbacks are invoked when the source completes
AND behavior is unchanged from current implementation
```

#### Test 7: ThrowIfFailed on Static Success
```
GIVEN HandlingResultPromise.Success
WHEN ThrowIfFailed() is called
THEN no exception is thrown (status is Success)
AND FailureContext is not mutated on the shared source
```

#### Test 8: WhenNotHandled on Static NoHandlers
```
GIVEN HandlingResultPromise.NoHandlers
AND a callback action
WHEN WhenNotHandled(callback) is called
THEN callback is invoked immediately
AND the static source is not mutated
```

### Race Condition Tests

#### Test 9: Parallel Access to Same Static Promise
```
GIVEN 50 threads accessing HandlingResultPromise.Success concurrently
WHEN each thread calls OnSuccess, OnCompletion, ThrowIfFailed
THEN all threads get correct results
AND no thread observes another thread's callback
```

#### Test 10: Mixed Static and Dynamic Promises
```
GIVEN both static and dynamic promises in use
WHEN callbacks are set on both
THEN static callbacks fire immediately
AND dynamic callbacks fire on completion
AND no cross-contamination
```

---

## Risks & Mitigations

| Risk | Mitigation |
|------|-----------|
| Silent callback loss if pattern changes | ImmutablePromiseSource makes it explicit — setters are no-ops |
| Breaking change if someone relies on stored callbacks | Static sources are always IsCompleted=true; callbacks are always invoked inline |
| Confusion about when callbacks are stored vs invoked | Document clearly in XML docs |

## Definition of Done

- [ ] ImmutablePromiseSource created and tested
- [ ] Static HandlingResultPromise fields use ImmutablePromiseSource
- [ ] Concurrent tests pass
- [ ] No callback loss under parallel access
- [ ] Existing tests pass unchanged
- [ ] XML docs explain behavior
