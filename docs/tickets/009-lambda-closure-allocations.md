# CHOP-009: Eliminate Lambda Closure Allocations in Registration Extensions

## Priority: 🟠 P1 — Major Performance
## Type: Performance / GC Optimization  
## Component: Native/Dependencies/Containers

---

## Problem Statement

Registration extension methods wrap user-provided factories in lambdas that capture variables, causing closure allocations:

```csharp
// IDependencyContainerExtensions.cs line 41
ImplementationFactory = c => dependency,          // Captures 'dependency' — closure alloc

// IDependencyContainerExtensions.cs line 82
ImplementationFactory = c => implementationFactory(c),  // Captures 'implementationFactory' — closure alloc
```

Every call to `Register<TContract>(container, dependency)` allocates:
1. A closure object to capture the `dependency` variable
2. A `Func<IDependencyContainer, object?>` delegate pointing to the closure

For the factory overload, it also wraps `implementationFactory` in another lambda (double indirection).

## Root Cause

**File:** `Native/Dependencies/Containers/IDependencyContainerExtensions.cs`

The `DependencySpecification.ImplementationFactory` is typed as `Func<IDependencyContainer, object?>`. When a generic lambda `c => dependency` captures a local variable, the compiler generates a display class (closure) on the heap.

## Solution Design

### Fix 1: Singleton Instance Registration — Avoid Closure

For the case where a known instance is registered:

```csharp
public static IDependencyContainer Register<TContract>(
    this IDependencyContainer container,
    TContract dependency) =>
        container.Register(new DependencySpecification()
        {
            Contract = typeof(TContract),
            ImplementationFactory = static _ => null,  // Placeholder
            Lifetime = DependencyLifetime.Singleton,
            // New: Direct instance for singleton case
            DirectInstance = dependency,
        }, out _);
```

Or, add a `DirectInstance` field to `DependencySpecification`:

```csharp
public readonly record struct DependencySpecification
{
    // Existing
    public required Func<IDependencyContainer, object?> ImplementationFactory { get; init; }
    
    // New: If set, this is used directly instead of invoking the factory
    public object? DirectInstance { get; init; }
}
```

Then in `SingletonResolution`:
```csharp
public override object? Get(IDependencyContainer container) =>
    _instance ??= _directInstance ?? Factory?.Invoke(container);
```

### Fix 2: Factory Wrapper — Remove Double Indirection

```csharp
// Current (allocates closure for implementationFactory):
ImplementationFactory = c => implementationFactory(c),

// Fixed (cast directly — same signature after erasure):
ImplementationFactory = (Func<IDependencyContainer, object?>)(object)implementationFactory,
```

Wait — this won't work because of return type variance. The user's factory returns `TContract` but `ImplementationFactory` expects `object?`. For reference types, covariant delegate conversion should work in newer C#. For value types, boxing is unavoidable at the factory level anyway.

Better approach: **Make the wrapping allocation happen once at registration** (which it already does) and accept it. The key insight is: registration is a cold path. The closure is created once and stored.

### Revised Assessment

The closure allocation happens **once per registration**, not once per resolution. Since registration is rare (startup, scene load), this is actually **P2, not P1** — unless you're registering/deregistering in hot loops.

However, for the `DirectInstance` pattern, we can still eliminate the factory entirely for pre-built singletons:

```csharp
// New resolution type for pre-built instances
public class DirectInstanceResolution(Type contract, object instance) :
    DependencyResolution(contract, null!)
{
    public override object? Get(IDependencyContainer container) => instance;
    
    public override void Dispose()
    {
        if (instance is IDisposable disposable)
            disposable.Dispose();
        base.Dispose();
    }
}
```

### Files to Modify

| File | Change |
|------|--------|
| `Native/Dependencies/Resolutions/DirectInstanceResolution.cs` | **NEW** — No-factory resolution |
| `Native/Dependencies/Containers/DependencySpecification.cs` | Add optional `DirectInstance` |
| `Native/Dependencies/Factories/DependencyResolutionFactory.cs` | Create DirectInstanceResolution when appropriate |
| `Native/Dependencies/Containers/IDependencyContainerExtensions.cs` | Use DirectInstance for pre-built deps |

---

## Acceptance Criteria

### Functional Requirements
- [ ] Pre-built singleton instances registered without factory allocation
- [ ] Direct instance resolution returns the exact instance (no factory indirection)
- [ ] Factory-based registration still works for transient/contained lifetimes
- [ ] All existing tests pass

### Performance Requirements
- [ ] Zero closure allocation for `Register<T>(container, instance)` pattern
- [ ] Resolution of direct instances is a single field read (fastest possible path)
- [ ] No regression for factory-based resolution

---

## Testing Specification

### Unit Tests

#### Test 1: Direct Instance Resolution
```
GIVEN container.Register<IService>(myServiceInstance)
WHEN Resolve<IService>(out var result) is called
THEN result is the exact same reference as myServiceInstance
AND no factory was invoked
```

#### Test 2: Direct Instance Disposal
```
GIVEN a direct instance resolution with an IDisposable instance
WHEN the container is disposed
THEN the instance's Dispose() is called
```

#### Test 3: No Closure Allocated
```
GIVEN a profiler tracking allocations
WHEN Register<IService>(container, instance) is called
THEN no System.Runtime.CompilerServices.Closure is allocated
```

#### Test 4: Factory Path Still Works
```
GIVEN Register<IService>(container, c => new Service(c.Resolve<IDep>()), Transient)
WHEN Resolve is called 3 times
THEN 3 different instances are created
```

#### Test 5: Direct Instance with Value Type
```
GIVEN container.Register<int>(42)
WHEN Resolve<int>(out var result) is called
THEN result == 42
```

### Benchmarks

```
[Benchmark]
public void Register_DirectInstance() => container.Register<IService>(instance);

[Benchmark]  
public void Register_WithClosure() => container.Register(new DependencySpecification() 
    { ImplementationFactory = c => instance, ... });
// Expected: DirectInstance path allocates less
```

---

## Definition of Done

- [ ] DirectInstanceResolution created and tested
- [ ] Registration extensions use DirectInstance for known instances
- [ ] Allocation benchmarks confirm reduction
- [ ] Existing tests pass
