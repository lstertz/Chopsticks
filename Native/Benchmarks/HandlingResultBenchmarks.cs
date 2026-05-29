using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using Chopsticks.Messages;
using Chopsticks.Messages.Handlers.Sources;

namespace Chopsticks.Benchmarks;

/// <summary>
/// Benchmarks for HandlingResult and HandlingResultPromise.
/// Tests CHOP-015 (readonly struct), CHOP-017 (static fields vs properties).
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class HandlingResultBenchmarks
{
    private HandlingResult _failureResult;
    private HandlingResult _multiFailureResult;

    [GlobalSetup]
    public void Setup()
    {
        _failureResult = HandlingResult.FromException(new InvalidOperationException("test"));
        
        var merged = HandlingResult.FromException(new InvalidOperationException("test1"))
            .MergeWith(HandlingResult.FromException(new ArgumentException("test2")))
            .MergeWith(HandlingResult.FromException(new NullReferenceException("test3")));
        _multiFailureResult = merged;
    }

    /// <summary>
    /// Access static Success property (currently evaluates new() each time).
    /// After CHOP-017: Should be a simple field read with zero allocation.
    /// </summary>
    [Benchmark(Description = "Static_Success_Access")]
    public HandlingResult Static_Success_Access()
    {
        return HandlingResult.Success;
    }

    /// <summary>
    /// Access static NoHandlers property.
    /// </summary>
    [Benchmark(Description = "Static_NoHandlers_Access")]
    public HandlingResult Static_NoHandlers_Access()
    {
        return HandlingResult.NoHandlers;
    }

    /// <summary>
    /// Access static Cancelled property.
    /// </summary>
    [Benchmark(Description = "Static_Cancelled_Access")]
    public HandlingResult Static_Cancelled_Access()
    {
        return HandlingResult.Cancelled;
    }

    /// <summary>
    /// High-frequency static access (1000x).
    /// Amplifies any per-access allocation from expression-bodied properties.
    /// </summary>
    [Benchmark(Description = "Static_Success_1000x")]
    public int Static_Success_Throughput()
    {
        int successCount = 0;
        for (int i = 0; i < 1000; i++)
        {
            var result = HandlingResult.Success;
            if (result.Status == HandlingStatus.Success)
                successCount++;
        }
        return successCount;
    }

    /// <summary>
    /// Enumerate exceptions from a single-exception failure (yield return overhead).
    /// </summary>
    [Benchmark(Description = "Exceptions_SingleException")]
    public int Exceptions_SingleException()
    {
        int count = 0;
        foreach (var ex in _failureResult.Exceptions)
            count++;
        return count;
    }

    /// <summary>
    /// Enumerate exceptions from a merged multi-exception failure.
    /// Tests yield return state machine allocation.
    /// </summary>
    [Benchmark(Description = "Exceptions_MultipleExceptions")]
    public int Exceptions_MultipleExceptions()
    {
        int count = 0;
        foreach (var ex in _multiFailureResult.Exceptions)
            count++;
        return count;
    }

    /// <summary>
    /// Access HandlingResultPromise.Success static property.
    /// Currently creates new TryHandlePromiseSource each time.
    /// </summary>
    [Benchmark(Description = "Promise_Static_Success")]
    public HandlingResultPromise Promise_Static_Success()
    {
        return HandlingResultPromise.Success;
    }

    /// <summary>
    /// Access HandlingResultPromise.NoHandlers static property.
    /// </summary>
    [Benchmark(Description = "Promise_Static_NoHandlers")]
    public HandlingResultPromise Promise_Static_NoHandlers()
    {
        return HandlingResultPromise.NoHandlers;
    }

    /// <summary>
    /// Tests HandlingResult struct copy semantics.
    /// After CHOP-015 (readonly struct), should have no defensive copies.
    /// </summary>
    [Benchmark(Description = "Struct_CopyAndAccess")]
    public HandlingStatus Struct_CopyAndAccess()
    {
        var result = HandlingResult.Success;
        var copy = result;
        return copy.Status;
    }
}
