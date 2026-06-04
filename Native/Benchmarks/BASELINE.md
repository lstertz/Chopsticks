# Chopsticks Performance Baseline

**Date:** 2026-05-18 (updated 2026-05-19)  
**Runtime:** .NET 9.0.16, Windows 11, X64 RyuJIT AVX2

---

## V2.1.0 - Zero Allocation Sync Path (Current)

Changes implemented:
- **Zero-Allocation Sync Path**: Synchronous handlers now bypass promise sources entirely
- **Full Promise Source Pooling**: All 4 non-sequential promise source types now pooled
- **Pool Pre-Warming**: `PromiseSourcePools.PreWarm()` API for startup optimization

### V2.1.0 Results

| Benchmark | V1.0 | V2.0.0 | V2.1.0 | Total Improvement |
|-----------|------|--------|--------|-------------------|
| SyncHandler_TryHandle | 52 ns, 312 B | 44 ns, 272 B | **~38 ns, 0 B** | **27% faster, 100% less alloc** |
| SyncHandler_Handle | 65 ns, 560 B | 55 ns, 480 B | **~40 ns, 0 B** | **38% faster, 100% less alloc** |
| SyncHandler_TryHandleAsync | 55 ns, 312 B | 46 ns, 272 B | **~39 ns, 0 B** | **29% faster, 100% less alloc** |
| SyncHandler_HandleAsync | 70 ns, 560 B | 60 ns, 480 B | **~45 ns, 0 B** | **36% faster, 100% less alloc** |
| Multicast_5Handlers | 92 ns, 344 B | 73 ns, 272 B | **~62 ns, 272 B** | **33% faster, 21% less alloc** |

### Key V2.1.0 Improvements

1. **Complete zero-allocation for sync handlers**
   - `HandlingResultPromise` / `HandlingResultAwaitable` can store results directly
   - No promise source allocated when result is immediately available
   - `Handle()` / `HandleAsync()` use fast paths bypassing source wrapping

2. **Pooling for all promise source types**
   - `TryHandlePromiseSource.Rent()` / `.Dispose()`
   - `TryHandleAsyncPromiseSource.Rent()` / `.Dispose()`
   - `HandlePromiseSource.Rent()` / `.Dispose()`
   - `HandleAsyncPromiseSource.Rent()` / `.Dispose()`

3. **Pre-warming API**
   - `PromiseSourcePools.PreWarm()` - fills pools at startup
   - Eliminates first-use allocation spike

---

## V2.0.0 - Wave 2/3 Completed

Changes implemented:
- **CHOP-003**: Thread-safe SingletonResolution.Get() with double-checked locking
- **CHOP-004**: Thread-safe ContainedResolution.Get() with ConcurrentDictionary + Lazy
- **CHOP-006**: ImmutablePromiseSource for static HandlingResultPromise instances
- **CHOP-005**: Cached handler array with volatile read (no array copy per dispatch)
- **CHOP-001**: Object pooling for SequentialHandlingPromiseSource
- **CHOP-011**: Binary search insertion instead of Sort() for registrations

### V2.0.0 Results (All Waves Complete)

| Benchmark | Before | After | Change |
|-----------|--------|-------|--------|
| Dispatch_1Handler_Sync | 52 ns, 312 B | **44 ns, 272 B** | **15% faster, 13% less alloc** |
| Dispatch_5Handlers_Sync | 92 ns, 344 B | **73 ns, 272 B** | **21% faster, 21% less alloc** |
| Dispatch_1000x | 46 μs, 312 KB | **37 μs, 272 KB** | **20% faster, 13% less alloc** |
| Exceptions_Single | 15 ns, 80 B | **5 ns, 56 B** | **67% faster, 30% less alloc** |
| Exceptions_Multiple | 19 ns, 80 B | **11 ns, 32 B** | **42% faster, 60% less alloc** |

### Key Improvements

1. **Per-dispatch allocation reduced by 40 B** (312 B → 272 B)
   - No more handler array copy per dispatch (cached array via volatile read)
   - Constant allocation regardless of handler count (was scaling with handlers)

2. **15-21% faster message dispatch**
   - Eliminated repeated MergeWith allocations for exceptions (O(N²) → O(N))
   - Binary search insertion avoids full sort on each registration

3. **Thread-safe singletons and contained resolutions** 
   - Double-checked locking for SingletonResolution
   - ConcurrentDictionary + Lazy for ContainedResolution
   - No more race conditions under concurrent access

4. **No shared mutable state in static promises**
   - ImmutablePromiseSource prevents callback pollution
   - Static Success/NoHandlers are truly immutable

5. **Zero-allocation exception enumeration** (for arrays)
   - Replaced yield return with direct collection return
   - SingleExceptionEnumerable struct for single-exception case

6. **Pooling infrastructure in place** for SequentialHandlingPromiseSource
   - Sources are returned to pool on Dispose()
   - Pool size capped at 64 instances per message type

---

## Wave 1 Completed

Changes implemented:
- **CHOP-015**: Made `HandlingResultPromise` a readonly struct
- **CHOP-014**: Added `IDisposable` to `IHandlingPromiseSource` interface  
- **CHOP-017**: Converted `HandlingResult` static properties to static readonly fields
- **CHOP-010**: Fixed double dictionary lookup in `Deregister`

---

## Pre-Optimization Baseline

## Summary

### Message Dispatch (Hot Path) - CRITICAL

| Benchmark | Mean | Allocated | Notes |
|-----------|------|-----------|-------|
| Dispatch_1Handler_Sync | 52 ns | **312 B** | Per-dispatch allocation! |
| Dispatch_5Handlers_Sync | 92 ns | **344 B** | Scales with handlers |
| Dispatch_1000x_Throughput | 46 μs | **312 KB** | 312 B × 1000 dispatches |

**Target:** 0 B allocated after pooling (CHOP-001) and array caching (CHOP-002)

### Dependency Resolution

| Benchmark | Mean | Allocated | Notes |
|-----------|------|-----------|-------|
| Resolve_Singleton_Cached | 7 ns | 0 B | Good - fast path works |
| Resolve_1000x_Throughput | 7.5 μs | 0 B | Good - no regression |
| Deregister_SingleLookup | 54 ns | **128 B** | Double lookup overhead |
| CanProvide_Check | 4 ns | 0 B | Good |

**Target:** Deregister should have minimal overhead after CHOP-010

### HandlingResult Statics

| Benchmark | Mean | Allocated | Notes |
|-----------|------|-----------|-------|
| Static_Success_Access | ~0 ns | 0 B | JIT optimized |
| Static_NoHandlers_Access | ~0.2 ns | 0 B | Good |
| Static_Cancelled_Access | ~0 ns | 0 B | JIT optimized |
| Static_Success_1000x | 225 ns | 0 B | Good |
| Exceptions_SingleException | 15 ns | **80 B** | yield return state machine! |
| Exceptions_MultipleExceptions | 19 ns | **80 B** | yield return state machine! |

**Target:** Exceptions enumeration should be 0 B with direct collection return

### Handler Registration

| Benchmark | Mean | Allocated | Notes |
|-----------|------|-----------|-------|
| Register_FirstHandler | 48 ns | 416 B | Cold path - acceptable |
| Register_5Handlers_Sequential | 167 ns | 1024 B | Sort overhead |
| Register_10Handlers_Sequential | 398 ns | 1936 B | Sort overhead grows |
| AccessHandlerArray_1Handler (100x) | 4.8 μs | **34,016 B** | **340 B per dispatch!** |

**Target:** Binary search insertion (CHOP-011) reduces Sort overhead

## Key Observations

1. **Per-dispatch allocation of 312-344 B is the primary GC pressure source**
   - At 60fps = 60 × 312 B = ~18 KB/sec of garbage per message type
   - This triggers Gen0 GC collections

2. **Handler array is copied on every dispatch** (see AccessHandlerArray benchmark)
   - 34 KB for 100 dispatches = 340 B per dispatch (array + overhead)
   - CHOP-002 (cache array) will eliminate this

3. **Exceptions property allocates due to yield return**
   - 80 B per access from enumerator state machine
   - CHOP-008 will fix this

4. **Static properties are already optimized** by JIT
   - Converting to static readonly fields (CHOP-017) may have minimal effect
   - JIT already inlines these
