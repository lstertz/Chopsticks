# Changelog v2.0.0 — Performance Release

**Release Date:** May 2026

## Overview

This release focuses on performance optimization, thread safety, and GC pressure reduction. All changes are backward compatible except for one interface change that requires client action.

---

## Performance Improvements

### Message Dispatch
- **15-21% faster** message dispatch
- **13-21% less allocation** per dispatch (312 B → 272 B)
- Allocation is now constant regardless of handler count

### Exception Handling  
- **42-67% faster** exception enumeration
- **30-60% less allocation** when accessing exceptions

### Throughput
- 1000 dispatches: 46 μs → 37 μs (**20% improvement**)
- 1000 resolutions: 7.5 μs → 9.3 μs (slight overhead for thread safety)

---

## Breaking Changes

### `IHandlingPromiseSource` now extends `IDisposable`

**Impact:** Medium  
**Action Required:** If you have custom implementations of `IHandlingPromiseSource`, you must add a `Dispose()` method.

```csharp
// Before (won't compile)
public class MySource : IHandlingPromiseSource { ... }

// After (add Dispose)
public class MySource : IHandlingPromiseSource
{
    public void Dispose() { /* cleanup */ }
}
```

---

## New Features

### Thread-Safe Dependency Resolution
- `SingletonResolution` now uses double-checked locking
- `ContainedResolution` now uses `ConcurrentDictionary`
- Safe for concurrent access from multiple threads

### Thread-Safe Handler Registration
- Handler registration is now protected by locks
- Cached handler array prevents per-dispatch allocation

### Object Pooling
- `SequentialHandlingPromiseSource` instances are pooled
- Pool size: 64 per message type
- Reduces GC pressure significantly

---

## Bug Fixes

### Fixed: Shared mutable state in static promises
Static `HandlingResultPromise.Success` and `NoHandlers` no longer share callback state between callers.

### Fixed: Race condition in singleton resolution
Concurrent calls to `Resolve<T>()` for singletons no longer invoke the factory multiple times.

### Fixed: Race condition in contained resolution
Concurrent calls from different containers no longer corrupt internal state.

### Fixed: O(N²) exception merge allocation
Sequential handler failures now accumulate exceptions in O(N) instead of O(N²).

---

## Internal Changes

- `HandlingResultPromise` is now a `readonly struct`
- `HandlingResult` static properties are now `static readonly` fields
- Binary search insertion replaces `Sort()` for registrations
- `yield return` replaced with direct collection returns

---

## Migration Checklist

- [ ] Add `Dispose()` to any custom `IHandlingPromiseSource` implementations
- [ ] Remove manual locking around dependency resolution (no longer needed)
- [ ] Review factory methods for side effects (now guaranteed single execution)
- [ ] Run memory profiler to verify allocation reduction
- [ ] Run concurrent tests to verify thread safety

---

## Full Documentation

See [PERFORMANCE-OPTIMIZATION-GUIDE.md](./PERFORMANCE-OPTIMIZATION-GUIDE.md) for detailed code changes, benchmarks, and migration guide.
