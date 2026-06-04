# Chopsticks Framework - Version Comparison

**Last Updated:** May 2026

---

## Quick Reference

| Metric | V1.0.0 (Original) | V2.0.0 | V2.1.0 (Current) |
|--------|-------------------|--------|------------------|
| **Sync TryHandle** | 52 ns, 312 B | 44 ns, 272 B | **38 ns, 0 B** |
| **Sync Handle** | 65 ns, 560 B | 55 ns, 480 B | **40 ns, 0 B** |
| **Multicast (5 handlers)** | 92 ns, 344 B | 73 ns, 272 B | **62 ns, 272 B** |
| **Thread Safety** | None | Full | Full |
| **Pooling** | None | Sequential only | **All sources** |
| **Pre-warming API** | No | No | **Yes** |

---

## Allocation Comparison

### Per-Operation Allocation (bytes)

```
                          V1.0.0    V2.0.0    V2.1.0
                          ──────    ──────    ──────
Sync TryHandle()          312 B     272 B       0 B  ████████████████████████████████░░░░░
Sync Handle()             560 B     480 B       0 B  ████████████████████████████████████████████████░░░░░
Sync TryHandleAsync()     312 B     272 B       0 B  ████████████████████████████████░░░░░
Sync HandleAsync()        560 B     480 B       0 B  ████████████████████████████████████████████████░░░░░
Async TryHandle()         312 B     272 B    ~128 B  ████████████████████████████████████████░░░░░░░░░░░░
Multicast (5 handlers)    344 B     272 B     272 B  █████████████████████████████████████████░░░░░░░░░░░░
Exception (single)         80 B      56 B      56 B  ████████████████████████████░░░░
Exception (array)          80 B      32 B      32 B  ████████████████░░░░░░░░░░░░░░░░
```

### Memory Pressure (60 FPS, 10 message types, sync handlers)

```
Version   Per-frame    Per-second    Per-minute    GC Frequency
───────   ─────────    ──────────    ──────────    ────────────
V1.0.0    3.12 KB      187 KB        11.2 MB       Every ~5 sec
V2.0.0    2.72 KB      163 KB         9.8 MB       Every ~6 sec
V2.1.0    0 KB           0 KB            0 MB       NEVER (from messaging)
```

---

## Feature Comparison

### V1.0.0 (Original)

- Basic message handling
- No pooling
- Not thread-safe
- Mutable shared state in static promises

### V2.0.0 (May 2026)

- **Thread-safe** singleton and contained resolution
- **Object pooling** for `SequentialHandlingPromiseSource`
- **Cached handler arrays** (no copy per dispatch)
- **Immutable static promises** (`ImmutablePromiseSource`)
- **Zero-allocation exception enumeration** (for arrays)
- **Binary search insertion** for handler registration
- **Linear exception accumulation** (O(N) vs O(N^2))

### V2.1.0 (May 2026) - Current

All V2.0.0 features plus:

- **Zero-allocation sync path** - sync handlers allocate 0 bytes
- **Full promise source pooling** - all 4 non-sequential types pooled
- **Pre-warming API** - `PromiseSourcePools.PreWarm()`
- **Direct result constructors** - bypasses promise sources entirely

---

## Breaking Changes by Version

### V2.0.0

| Change | Action Required |
|--------|----------------|
| `IHandlingPromiseSource` extends `IDisposable` | Implement `Dispose()` in custom sources |

### V2.1.0

| Change | Action Required |
|--------|----------------|
| None | No changes required |

---

## Migration Paths

### V1.0.0 -> V2.1.0 (Recommended)

1. Implement `IDisposable` on custom promise sources
2. Remove manual locking around resolution (now thread-safe)
3. Call `PromiseSourcePools.PreWarm()` at startup (optional)

### V2.0.0 -> V2.1.0

1. Call `PromiseSourcePools.PreWarm()` at startup (optional)
2. No other changes required

---

## Documentation

| Version | Document |
|---------|----------|
| V2.1.0 | [PERFORMANCE-OPTIMIZATION-GUIDE-V2.1.md](./PERFORMANCE-OPTIMIZATION-GUIDE-V2.1.md) |
| V2.0.0 | [PERFORMANCE-OPTIMIZATION-GUIDE.md](./PERFORMANCE-OPTIMIZATION-GUIDE.md) |
| Baseline | [Native/Benchmarks/BASELINE.md](../Native/Benchmarks/BASELINE.md) |

---

## Changelog

### V2.1.0 (May 2026)
- Zero-allocation sync handlers via direct result constructors
- Added pooling to `TryHandlePromiseSource`, `TryHandleAsyncPromiseSource`, `HandlePromiseSource`, `HandleAsyncPromiseSource`
- New `PromiseSourcePools.PreWarm()` API

### V2.0.0 (May 2026)
- Thread-safe singleton and contained dependency resolution
- Object pooling for `SequentialHandlingPromiseSource`
- Cached handler array with volatile read
- `ImmutablePromiseSource` for static promise instances
- Zero-allocation exception enumeration
- Binary search handler registration
- Linear exception accumulation
- `IHandlingPromiseSource` extends `IDisposable`
- `HandlingResultPromise` converted to `readonly struct`

### V1.0.0
- Initial release
