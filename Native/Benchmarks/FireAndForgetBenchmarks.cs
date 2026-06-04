using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Order;
using Chopsticks.Messages;
using Chopsticks.Messages.Handlers;
using Chopsticks.Messages.Handlers.Multicast;
using Chopsticks.Messages.Handlers.Sources;
using Chopsticks.Messages.Registration;

namespace Chopsticks.Benchmarks;

/// <summary>
/// Benchmarks comparing fire-and-forget API vs unconsumed TryHandle.
/// Demonstrates allocation savings from the new zero-allocation API.
/// </summary>
[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class FireAndForgetBenchmarks
{
    private MulticastMessageHandler<BenchMessage> _handler1 = null!;
    private MulticastMessageHandler<BenchMessage> _handler5 = null!;
    private BenchMessage _message = default;

    [GlobalSetup]
    public void Setup()
    {
        _message = new BenchMessage(42, 3.14f);

        _handler1 = new MulticastMessageHandler<BenchMessage>();
        ((IMessageHandlerRegistrar<BenchMessage>)_handler1).Register(new FastSyncHandler(), default);

        _handler5 = new MulticastMessageHandler<BenchMessage>();
        for (int i = 0; i < 5; i++)
            ((IMessageHandlerRegistrar<BenchMessage>)_handler5).Register(new FastSyncHandler(), default);
    }

    // ==========================================================================
    // SINGLE HANDLER BENCHMARKS
    // ==========================================================================

    [Benchmark(Baseline = true, Description = "TryHandle (unconsumed)")]
    [BenchmarkCategory("1 Handler")]
    public void SingleHandler_TryHandle_Unconsumed()
    {
        _handler1.TryHandle(_message);
    }

    [Benchmark(Description = "TryHandleFireAndForget")]
    [BenchmarkCategory("1 Handler")]
    public void SingleHandler_FireAndForget()
    {
        _handler1.TryHandleFireAndForget(_message);
    }

    [Benchmark(Description = "HandleSync")]
    [BenchmarkCategory("1 Handler")]
    public HandlingResult SingleHandler_HandleSync()
    {
        return _handler1.HandleSync(_message);
    }

    [Benchmark(Description = "TryHandle (consumed)")]
    [BenchmarkCategory("1 Handler")]
    public HandlingStatus SingleHandler_TryHandle_Consumed()
    {
        return _handler1.TryHandle(_message).Status;
    }

    // ==========================================================================
    // 5 HANDLERS BENCHMARKS
    // ==========================================================================

    [Benchmark(Baseline = true, Description = "TryHandle (unconsumed)")]
    [BenchmarkCategory("5 Handlers")]
    public void FiveHandlers_TryHandle_Unconsumed()
    {
        _handler5.TryHandle(_message);
    }

    [Benchmark(Description = "TryHandleFireAndForget")]
    [BenchmarkCategory("5 Handlers")]
    public void FiveHandlers_FireAndForget()
    {
        _handler5.TryHandleFireAndForget(_message);
    }

    [Benchmark(Description = "HandleSync")]
    [BenchmarkCategory("5 Handlers")]
    public HandlingResult FiveHandlers_HandleSync()
    {
        return _handler5.HandleSync(_message);
    }

    [Benchmark(Description = "TryHandle (consumed)")]
    [BenchmarkCategory("5 Handlers")]
    public HandlingStatus FiveHandlers_TryHandle_Consumed()
    {
        return _handler5.TryHandle(_message).Status;
    }

    // ==========================================================================
    // THROUGHPUT BENCHMARKS (1000 iterations)
    // ==========================================================================

    [Benchmark(Baseline = true, Description = "TryHandle (unconsumed) x1000")]
    [BenchmarkCategory("Throughput")]
    public void Throughput_TryHandle_Unconsumed()
    {
        for (int i = 0; i < 1000; i++)
            _handler5.TryHandle(_message);
    }

    [Benchmark(Description = "TryHandleFireAndForget x1000")]
    [BenchmarkCategory("Throughput")]
    public void Throughput_FireAndForget()
    {
        for (int i = 0; i < 1000; i++)
            _handler5.TryHandleFireAndForget(_message);
    }

    [Benchmark(Description = "HandleSync x1000")]
    [BenchmarkCategory("Throughput")]
    public int Throughput_HandleSync()
    {
        int successCount = 0;
        for (int i = 0; i < 1000; i++)
        {
            if (_handler5.HandleSync(_message).Status == HandlingStatus.Success)
                successCount++;
        }
        return successCount;
    }

    [Benchmark(Description = "TryHandle (consumed) x1000")]
    [BenchmarkCategory("Throughput")]
    public int Throughput_TryHandle_Consumed()
    {
        int successCount = 0;
        for (int i = 0; i < 1000; i++)
        {
            if (_handler5.TryHandle(_message).Status == HandlingStatus.Success)
                successCount++;
        }
        return successCount;
    }
}

public readonly struct BenchMessage
{
    public readonly int Id;
    public readonly float Value;

    public BenchMessage(int id, float value)
    {
        Id = id;
        Value = value;
    }
}

public class FastSyncHandler : ISyncMessageHandler<BenchMessage>
{
    public void Handle(BenchMessage message)
    {
        // Minimal work - just validates message
        _ = message.Id + message.Value;
    }
}
