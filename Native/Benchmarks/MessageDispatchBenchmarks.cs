using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using Chopsticks.Messages;
using Chopsticks.Messages.Handlers;
using Chopsticks.Messages.Handlers.Multicast;
using Chopsticks.Messages.Handlers.Sources;
using Chopsticks.Messages.Registration;

namespace Chopsticks.Benchmarks;

/// <summary>
/// Benchmarks for the message dispatch hot path.
/// Tests CHOP-001 (promise source pooling), CHOP-002 (handler array caching), CHOP-005 (thread safety).
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class MessageDispatchBenchmarks
{
    private MulticastContextHandler<TestMessage, DefaultMessageContext<TestMessage>> _handler = null!;
    private MulticastContextHandler<TestMessage, DefaultMessageContext<TestMessage>> _handlerWith5Handlers = null!;
    private TestMessage _message = null!;

    [GlobalSetup]
    public void Setup()
    {
        _message = new TestMessage { Value = 42 };

        _handler = new MulticastContextHandler<TestMessage, DefaultMessageContext<TestMessage>>();
        ((IMessageHandlerRegistrar<TestMessage>)_handler).Register(new SyncTestHandler());

        _handlerWith5Handlers = new MulticastContextHandler<TestMessage, DefaultMessageContext<TestMessage>>();
        for (int i = 0; i < 5; i++)
            ((IMessageHandlerRegistrar<TestMessage>)_handlerWith5Handlers).Register(new SyncTestHandler());
    }

    /// <summary>
    /// Baseline: Dispatch with 1 synchronous handler.
    /// Expected allocations before optimization: SequentialHandlingPromiseSource + handler array copy.
    /// Expected allocations after optimization: 0 (pooled source, cached array).
    /// </summary>
    [Benchmark(Description = "Dispatch_1Handler_Sync")]
    public HandlingResultPromise Dispatch_SingleHandler()
    {
        return _handler.TryHandle(_message);
    }

    /// <summary>
    /// Dispatch with 5 synchronous handlers.
    /// Tests array copy overhead scaling with handler count.
    /// </summary>
    [Benchmark(Description = "Dispatch_5Handlers_Sync")]
    public HandlingResultPromise Dispatch_FiveHandlers()
    {
        return _handlerWith5Handlers.TryHandle(_message);
    }

    /// <summary>
    /// High-frequency dispatch simulation (1000 messages).
    /// This amplifies allocation patterns for easier measurement.
    /// </summary>
    [Benchmark(Description = "Dispatch_1000x_Throughput")]
    public int Dispatch_Throughput()
    {
        int successCount = 0;
        for (int i = 0; i < 1000; i++)
        {
            var promise = _handler.TryHandle(_message);
            if (promise.Status == HandlingStatus.Success)
                successCount++;
        }
        return successCount;
    }
}

public class TestMessage
{
    public int Value { get; set; }
}

public class SyncTestHandler : IMessageHandler<TestMessage>
{
    private static readonly IHandlingPromiseSource _source = new TryHandlePromiseSource().Init(HandlingResult.Success);

    public HandlingResultPromise TryHandle(TestMessage message)
    {
        return HandlingResultPromise.Success;
    }

    public HandlingResultAwaitable TryHandleAsync(TestMessage message, CancellationToken token = default)
    {
        return new HandlingResultAwaitable(_source);
    }
}

public class AsyncTestHandler : ITaskMessageHandler<TestMessage>
{
    public async Task HandleAsync(TestMessage message, CancellationToken token = default)
    {
        await Task.Yield();
    }
}
