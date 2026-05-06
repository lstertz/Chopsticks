# CHOP-005: Fix Race Condition — Handler Registration vs Dispatch

## Priority: 🔴 P0 — Critical
## Type: Bug / Thread Safety
## Component: Native/Messages/Registration

---

## Problem Statement

`BaseMessageHandlerRegistrar` has a `List<BaseRegisteredHandler<TMessage, TContext>>` that is:
- **Mutated** during `AddRegistration()` and `RemoveRegistration()`
- **Read** during `RegisteredMessageHandlers` property (which copies to array for dispatch)

There is **zero synchronization** between these operations. If handler registration occurs on one thread while message dispatch occurs on another thread:

1. `List<T>.Add()` may trigger a resize while another thread is iterating
2. `List<T>.Sort()` reorders elements while another thread copies via spread operator
3. `List<T>.Remove()` shifts elements while dispatch reads the list

The `volatile` on `_dispatchPipeline` in `BaseMulticastHandler` shows awareness of threading concerns, but the handler list mutation + pipeline rebuild is not protected.

## Root Cause

**File:** `Native/Messages/Registration/BaseMessageHandlerRegistrar.cs`

```csharp
private readonly List<BaseRegisteredHandler<TMessage, TContext>> _registeredMessageHandlers = new(8);

// MUTATION (any thread)
private void AddRegistration(BaseRegisteredHandler<TMessage, TContext> registration)
{
    _registeredMessageHandlers.Add(registration);       // Unsafe
    _registeredMessageHandlers.Sort((x, y) => ...);    // Unsafe
}

// READ (dispatch thread)  
protected BaseRegisteredHandler<TMessage, TContext>[] RegisteredMessageHandlers => 
    [.. _registeredMessageHandlers];  // Reads during mutation = corruption
```

## Solution Design

### Implementation: Lock Registration, Atomic Publish

This builds on CHOP-002 (cached array). The pattern is:
1. Lock around all mutations to `_registeredMessageHandlers`
2. After mutation, atomically publish the new snapshot array
3. Dispatch reads the volatile snapshot — no lock needed

```csharp
public abstract class BaseMessageHandlerRegistrar<TMessage, TContext>
    where TContext : IMessageContext<TMessage>, new()
{
    private readonly object _registrationLock = new();
    private readonly List<BaseRegisteredHandler<TMessage, TContext>> _registeredMessageHandlers = new(8);
    
    // Atomically published snapshot — dispatch reads this without locking
    protected BaseRegisteredHandler<TMessage, TContext>[] RegisteredMessageHandlers => _cachedHandlers;
    private volatile BaseRegisteredHandler<TMessage, TContext>[] _cachedHandlers = [];


    private void AddRegistration(BaseRegisteredHandler<TMessage, TContext> registration)
    {
        lock (_registrationLock)
        {
            if (_registeredMessageHandlers.Contains(registration))
                return;

            _registeredMessageHandlers.Add(registration);
            _registeredMessageHandlers.Sort((x, y) =>
            {
                int orderComparison = x.Order.CompareTo(y.Order);
                if (orderComparison != 0) return orderComparison;
                return x.RegistrationIndex.CompareTo(y.RegistrationIndex);
            });

            // Atomic publish — readers see complete snapshot or previous snapshot
            _cachedHandlers = [.. _registeredMessageHandlers];
        }
    }

    protected void RemoveRegistration(IRegisteredHandler registration)
    {
        lock (_registrationLock)
        {
            if (registration is not BaseRegisteredHandler<TMessage, TContext> reg)
                return;

            _registeredMessageHandlers.Remove(reg);
            _cachedHandlers = [.. _registeredMessageHandlers];
        }
    }
}
```

### Same Pattern for Interceptors

Apply identical pattern to `_dispatchInterceptors` list in the same class:

```csharp
public IRegisteredInterceptor AddDispatchInterceptor(
    IContextInterceptor<TMessage, TContext> interceptor,
    InterceptorRegistrationSettings settings = default)
{
    lock (_registrationLock)
    {
        // ... add and sort ...
        RebuildDispatchPipeline(_dispatchInterceptors);
    }
    return registration;
}
```

### Files to Modify

| File | Change |
|------|--------|
| `Native/Messages/Registration/BaseMessageHandlerRegistrar.cs` | Add lock, atomic publish |
| `Native/Messages/Registration/Handlers/BaseRegisteredHandler.cs` | Same pattern for per-handler interceptors |

---

## Acceptance Criteria

### Functional Requirements
- [ ] Handler registration during dispatch does not corrupt state
- [ ] Handler deregistration during dispatch does not cause exceptions
- [ ] In-flight dispatches complete with the handler set that was active at dispatch start
- [ ] New dispatches after registration see the updated handler set
- [ ] Interceptor registration follows the same safety guarantees

### Performance Requirements
- [ ] Dispatch path acquires NO locks (volatile read only)
- [ ] Registration lock is held only during mutation (not during dispatch)
- [ ] No lock contention between concurrent dispatches

---

## Testing Specification

### Unit Tests

#### Test 1: Register During Sequential Dispatch
```
GIVEN a handler that takes 100ms to complete
AND a second handler registered while the first dispatch is in progress
WHEN the first dispatch started before registration
THEN the first dispatch only invokes the original handler set
AND a subsequent dispatch invokes both handlers
```

#### Test 2: Deregister During Sequential Dispatch
```
GIVEN two handlers registered
AND a dispatch is in progress (handler 1 is executing)
WHEN handler 2 is deregistered during handler 1's execution
THEN the in-progress dispatch still invokes handler 2 (snapshot semantics)
AND the next dispatch only invokes handler 1
```

#### Test 3: Concurrent Registration Safety
```
GIVEN an empty registrar
WHEN 10 threads each register a unique handler concurrently
THEN all 10 handlers are registered successfully
AND RegisteredMessageHandlers.Length == 10
AND ordering is correct
```

#### Test 4: Concurrent Deregistration Safety
```
GIVEN a registrar with 10 handlers
WHEN 10 threads each deregister a different handler concurrently
THEN all handlers are deregistered
AND RegisteredMessageHandlers.Length == 0
AND no exception is thrown
```

#### Test 5: Mixed Register/Deregister/Dispatch Concurrent
```
GIVEN a registrar with 5 handlers
WHEN for 2 seconds:
  - 5 threads continuously dispatch messages
  - 2 threads continuously register new handlers
  - 2 threads continuously deregister random handlers
THEN no exceptions are thrown
AND all dispatches complete with valid HandlingResult
AND final handler count is consistent with operations performed
```

#### Test 6: Snapshot Immutability
```
GIVEN a dispatch in progress holding a reference to the handler array
WHEN registration modifies the handler list
THEN the held array reference is unchanged (old snapshot)
AND a new array is published for future dispatches
```

#### Test 7: Interceptor Registration Safety
```
GIVEN a registrar with interceptors
WHEN interceptors are registered/removed concurrently with dispatch
THEN the dispatch pipeline is consistent
AND no NullReferenceException from stale pipeline references
```

### Stress Tests

#### Test 8: 60fps Dispatch + Occasional Registration
```
GIVEN a registrar with 3 handlers
WHEN 60 dispatches per second occur continuously
AND a handler is registered/deregistered every 500ms
THEN over 30 seconds: no exceptions, no missed handlers, no corruption
AND all dispatches return valid results
```

#### Test 9: Registration Storm
```
GIVEN an empty registrar
WHEN 100 handlers are registered in rapid succession from one thread
AND another thread dispatches continuously
THEN no List<T> resize corruption
AND handler counts monotonically increase as seen by dispatch
```

#### Test 10: Deadlock Detection
```
GIVEN a handler that itself registers another handler during its execution
WHEN a message is dispatched triggering that handler
THEN no deadlock occurs (lock is reentrant or registration is deferred)
```

### Edge Cases

#### Test 11: Empty Registrar Dispatch
```
GIVEN a registrar with no handlers
WHEN dispatch is called
THEN HandlingResult.NoHandlers is returned
AND no exception from empty array access
```

#### Test 12: Single Handler Registered and Immediately Dispatched
```
GIVEN an empty registrar
WHEN register + dispatch are called back-to-back on the same thread
THEN the dispatch sees the newly registered handler
```

---

## Risks & Mitigations

| Risk | Mitigation |
|------|-----------|
| Re-entrant lock (handler registers during dispatch) | Dispatch doesn't hold the lock; registration during handler execution is safe |
| Lock contention on rapid registration | Registration is rare vs dispatch; lock scope is minimal |
| Memory: old snapshots kept alive by in-flight dispatch | Snapshot is released after dispatch completes; GC handles it |
| Array allocation on every registration | Acceptable — registration is cold path; dispatch is hot path |

## Dependencies

- **CHOP-002**: This ticket subsumes CHOP-002's changes (cached array is part of this solution)

## Definition of Done

- [ ] All unit tests pass
- [ ] All stress tests pass over extended duration (30+ seconds)
- [ ] No deadlocks detected in deadlock detection test
- [ ] Dispatch path confirmed to acquire no locks (code review)
- [ ] ThreadSanitizer or equivalent shows no data races
- [ ] Existing test suite passes unchanged
