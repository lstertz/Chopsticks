using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using Chopsticks.Messages;
using Chopsticks.Messages.Handlers;
using Chopsticks.Messages.Handlers.Multicast;
using Chopsticks.Messages.Registration;
using Chopsticks.Messages.Registration.Handlers;

namespace Chopsticks.Benchmarks;

/// <summary>
/// Benchmarks for handler registration.
/// Tests CHOP-011 (binary search insertion vs full sort).
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class RegistrationBenchmarks
{
    private IMessageHandler<TestMessage>[] _handlers = null!;

    [GlobalSetup]
    public void Setup()
    {
        _handlers = new IMessageHandler<TestMessage>[20];
        for (int i = 0; i < 20; i++)
            _handlers[i] = new SyncTestHandler();
    }

    /// <summary>
    /// Register a single handler to an empty registrar.
    /// Baseline for registration overhead.
    /// </summary>
    [Benchmark(Description = "Register_FirstHandler")]
    public IRegisteredHandler<TestMessage> Register_FirstHandler()
    {
        var handler = new MulticastContextHandler<TestMessage, DefaultMessageContext<TestMessage>>();
        return ((IMessageHandlerRegistrar<TestMessage>)handler).Register(_handlers[0]);
    }

    /// <summary>
    /// Register 5 handlers sequentially.
    /// Tests Sort() being called 5 times.
    /// </summary>
    [Benchmark(Description = "Register_5Handlers_Sequential")]
    public int Register_5Handlers()
    {
        var handler = new MulticastContextHandler<TestMessage, DefaultMessageContext<TestMessage>>();
        var registrar = (IMessageHandlerRegistrar<TestMessage>)handler;
        
        for (int i = 0; i < 5; i++)
            registrar.Register(_handlers[i]);
        
        return 5;
    }

    /// <summary>
    /// Register 10 handlers sequentially.
    /// Tests Sort() overhead with larger list.
    /// </summary>
    [Benchmark(Description = "Register_10Handlers_Sequential")]
    public int Register_10Handlers()
    {
        var handler = new MulticastContextHandler<TestMessage, DefaultMessageContext<TestMessage>>();
        var registrar = (IMessageHandlerRegistrar<TestMessage>)handler;
        
        for (int i = 0; i < 10; i++)
            registrar.Register(_handlers[i]);
        
        return 10;
    }

    /// <summary>
    /// Register handlers with varying order values.
    /// Tests sorting behavior with actual ordering requirements.
    /// </summary>
    [Benchmark(Description = "Register_WithVaryingOrder")]
    public int Register_WithVaryingOrder()
    {
        var handler = new MulticastContextHandler<TestMessage, DefaultMessageContext<TestMessage>>();
        var registrar = (IMessageHandlerRegistrar<TestMessage>)handler;
        
        registrar.Register(_handlers[0], new HandlerRegistrationSettings { Order = 5 });
        registrar.Register(_handlers[1], new HandlerRegistrationSettings { Order = 1 });
        registrar.Register(_handlers[2], new HandlerRegistrationSettings { Order = 3 });
        registrar.Register(_handlers[3], new HandlerRegistrationSettings { Order = 2 });
        registrar.Register(_handlers[4], new HandlerRegistrationSettings { Order = 4 });
        
        return 5;
    }

    /// <summary>
    /// Access RegisteredMessageHandlers array (currently copies via spread operator).
    /// This is called on every dispatch, so it's performance-critical.
    /// </summary>
    [Benchmark(Description = "AccessHandlerArray_1Handler")]
    public int AccessHandlerArray_1Handler()
    {
        var handler = new MulticastContextHandler<TestMessage, DefaultMessageContext<TestMessage>>();
        ((IMessageHandlerRegistrar<TestMessage>)handler).Register(_handlers[0]);
        
        int count = 0;
        for (int i = 0; i < 100; i++)
        {
            var promise = handler.TryHandle(new TestMessage());
            if (promise.Status == HandlingStatus.Success)
                count++;
        }
        return count;
    }
}
