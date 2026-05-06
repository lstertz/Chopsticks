# CHOP-007: Generic DependencyResolution<T> to Eliminate Boxing

## Priority: 🟠 P1 — Major Performance
## Type: Architecture / Performance
## Component: Native/Dependencies

---

## Problem Statement

The entire DI resolution pipeline funnels through `object?`:

```csharp
// DependencyResolution.cs
public abstract object? Get(IDependencyContainer container);

// IDependencyContainer.cs
bool Resolve(Type contract, out object? implementation);

// IDependencyContainerExtensions.cs
public static bool Resolve<TContract>(this IDependencyContainer container, out TContract? implementation)
{
    var wasResolved = container.Resolve(typeof(TContract), out var uncastImplementation);
    // ^^^ Boxing happens here if TContract is a value type
    if (uncastImplementation is not TContract castImplementation) { ... }
    // ^^^ Unboxing + type check
}
```

**If any value type (struct, int, enum, custom struct) is registered as a dependency, every single resolution boxes it.** Boxing allocates ~16-24 bytes on the heap and creates GC pressure.

Even for reference types, the pattern match `is TContract` generates unnecessary type checks that the JIT may not fully elide.

## Root Cause

**Files:** 
- `Native/Dependencies/Resolutions/DependencyResolution.cs` — returns `object?`
- `Native/Dependencies/Containers/IDependencyContainer.cs` — interface uses `object?`
- `Native/Dependencies/Containers/DependencyContainer.cs` — implementation uses `object?`
- `Native/Dependencies/Containers/IDependencyContainerExtensions.cs` — generic wrappers box/unbox

The original design uses runtime typing (`Type contract`) for maximum flexibility, but pays the boxing tax on value types.

## Solution Design

### Approach: Add Generic Resolution Path Alongside Existing Non-Generic Path

Keep backward compatibility by maintaining the `object?` path, but add a faster generic path:

```csharp
// New interface method (or extension)
public interface IDependencyContainer
{
    // Existing (keep for backward compat and runtime-typed resolution)
    bool Resolve(Type contract, out object? implementation);
    
    // New: zero-boxing generic resolution
    bool Resolve<TContract>(out TContract? implementation);
}
```

### Implementation in DependencyContainer

```csharp
public bool Resolve<TContract>(out TContract? implementation)
{
    implementation = default;
    
    var resolution = (this as IDependencyResolutionProvider).GetResolution(typeof(TContract));
    if (resolution == null)
        return false;

    // If resolution can provide typed access, use it
    if (resolution is ITypedResolution<TContract> typedResolution)
    {
        implementation = typedResolution.Get(this);
        return true;
    }
    
    // Fallback to object path (boxing for value types)
    var obj = resolution.Get(this);
    if (obj is TContract typed)
    {
        implementation = typed;
        return true;
    }
    
    return false;
}
```

### Typed Resolution Interface

```csharp
// New file: Native/Dependencies/Resolutions/ITypedResolution.cs
public interface ITypedResolution<T>
{
    T? Get(IDependencyContainer container);
}
```

### Typed Singleton Resolution

```csharp
public class SingletonResolution<T>(Func<IDependencyContainer, T?> factory) :
    DependencyResolution(typeof(T), c => factory(c)),
    ITypedResolution<T>
{
    private T? _instance;
    private readonly object _lock = new();
    private bool _isCreated;

    T? ITypedResolution<T>.Get(IDependencyContainer container)
    {
        if (_isCreated) return _instance;
        lock (_lock)
        {
            if (_isCreated) return _instance;
            _instance = factory(container);
            _isCreated = true;
            return _instance;
        }
    }

    public override object? Get(IDependencyContainer container) =>
        ((ITypedResolution<T>)this).Get(container);
}
```

### Generic Registration Extension

```csharp
public static IDependencyContainer Register<TContract>(
    this IDependencyContainer container,
    Func<IDependencyContainer, TContract> implementationFactory,
    DependencyLifetime lifetime = DependencyLifetime.Singleton)
{
    // Create typed resolution to avoid boxing
    var spec = new DependencySpecification()
    {
        Contract = typeof(TContract),
        ImplementationFactory = c => implementationFactory(c),
        Lifetime = lifetime,
    };
    return container.Register(spec, out _);
}
```

### Files to Modify/Create

| File | Change |
|------|--------|
| `Native/Dependencies/Resolutions/ITypedResolution.cs` | **NEW** — Generic resolution interface |
| `Native/Dependencies/Resolutions/SingletonResolution.cs` | Add `ITypedResolution<T>` variant or type parameter |
| `Native/Dependencies/Resolutions/TransientResolution.cs` | Same |
| `Native/Dependencies/Resolutions/ContainedResolution.cs` | Same |
| `Native/Dependencies/Containers/IDependencyContainer.cs` | Add generic `Resolve<T>` |
| `Native/Dependencies/Containers/DependencyContainer.cs` | Implement generic resolve |
| `Native/Dependencies/Containers/IDependencyContainerExtensions.cs` | Use generic path when available |
| `Native/Dependencies/Factories/DependencyResolutionFactory.cs` | Build typed resolutions |

---

## Acceptance Criteria

### Functional Requirements
- [ ] Value type dependencies resolved without boxing
- [ ] Reference type dependencies still work (no regression)
- [ ] Generic resolve path produces identical results to non-generic path
- [ ] All existing tests pass without modification
- [ ] Backward-compatible — non-generic path still available

### Performance Requirements
- [ ] Zero heap allocations when resolving value-type singletons (after first resolution)
- [ ] No boxing observed in BenchmarkDotNet allocation report
- [ ] Reference type resolution not slower than current implementation

---

## Testing Specification

### Unit Tests

#### Test 1: Value Type Resolved Without Boxing
```
GIVEN a container with registered int dependency (value = 42)
WHEN Resolve<int>(out var result) is called
THEN result == 42
AND no heap allocation occurs (verified via GC.GetAllocatedBytesForCurrentThread)
```

#### Test 2: Struct Resolved Without Boxing
```
GIVEN a container with registered custom struct (Vector3 equivalent)
WHEN Resolve<CustomStruct>(out var result) is called
THEN result contains correct values
AND no boxing allocation
```

#### Test 3: Reference Type Still Works
```
GIVEN a container with registered class dependency
WHEN Resolve<IMyService>(out var result) is called
THEN result is the expected instance
AND behavior is identical to non-generic path
```

#### Test 4: Generic and Non-Generic Paths Are Consistent
```
GIVEN a container with dependency X
WHEN Resolve<X>(out var genericResult) is called
AND Resolve(typeof(X), out var nonGenericResult) is called
THEN genericResult and nonGenericResult reference the same object
```

#### Test 5: Typed Singleton Thread Safety
```
GIVEN a typed SingletonResolution<int>
WHEN 20 threads call Get concurrently
THEN all receive the same value
AND the factory is invoked exactly once
```

#### Test 6: Typed Transient Creates New Each Time
```
GIVEN a typed TransientResolution<MyStruct>
AND factory increments a counter per creation
WHEN Get is called 5 times
THEN factory is invoked 5 times
AND each result may be different (if factory produces unique values)
```

#### Test 7: Fallback to Object Path
```
GIVEN a resolution that does NOT implement ITypedResolution<T>
WHEN Resolve<T> is called
THEN it falls back to the object? path
AND still returns the correct result (with boxing for value types)
```

#### Test 8: Enum Resolution Without Boxing
```
GIVEN a container with registered enum value (MyEnum.Active)
WHEN Resolve<MyEnum>(out var result) is called
THEN result == MyEnum.Active
AND zero heap allocation
```

### Performance Benchmarks

#### Benchmark 1: Boxing Elimination
```
[Benchmark(Baseline = true)]
public int Resolve_NonGeneric_ValueType()
{
    container.Resolve(typeof(int), out var obj);
    return (int)obj!;  // Boxes + Unboxes
}

[Benchmark]
public int Resolve_Generic_ValueType()
{
    container.Resolve<int>(out var result);
    return result;  // No boxing
}
// Expected: Generic path shows 0 Gen0 allocations
```

#### Benchmark 2: Reference Type Parity
```
[Benchmark]
public IService Resolve_Generic_RefType() { ... }

[Benchmark]
public IService Resolve_NonGeneric_RefType() { ... }
// Expected: Within 5% of each other
```

---

## Risks & Mitigations

| Risk | Mitigation |
|------|-----------|
| Large API surface increase | Only add generic methods to public interface; internals can be refactored later |
| Breaking change for custom resolution implementations | ITypedResolution is optional; fallback to object path |
| Complexity in resolution factory | Type-parametric factory methods with constraints |
| DependencySpecification still uses object? | Keep for backward compat; typed path is optimization layer |

## Dependencies

- **CHOP-003**: Singleton thread safety should be done first (this ticket builds on it)

## Definition of Done

- [ ] Generic Resolve<T> available on IDependencyContainer
- [ ] Value type resolution shows zero allocations in benchmark
- [ ] All existing tests pass
- [ ] New typed resolution classes created and tested
- [ ] XML documentation complete
- [ ] Backward compatibility verified
