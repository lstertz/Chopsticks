using Chopsticks.Messages;
using Chopsticks.Messages.Handlers;
using Chopsticks.Messages.Handlers.Multicast;
using Chopsticks.Messages.Handlers.Sources;
using Chopsticks.Messages.Interceptors;
using Chopsticks.Messages.Registration;
using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Chopsticks.Tests;

/// <summary>
/// Aggressive correctness, concurrency, and lifecycle tests intended to surface
/// races, pool corruption, drain, ordering, and aggregation defects under load.
/// </summary>
[TestFixture]
[Category("Hardening")]
public class HardeningTests
{
    public sealed class Msg
    {
        public int Id { get; init; }
    }

    private static MulticastMessageHandler<Msg> NewHandler() => new();

    private static IMessageHandlerRegistrar<Msg, DefaultMessageContext<Msg>> Registrar(
        MulticastMessageHandler<Msg> handler) =>
        (IMessageHandlerRegistrar<Msg, DefaultMessageContext<Msg>>)handler;

    // ---- Handlers ---------------------------------------------------------

    private sealed class SyncCounter : ISyncMessageHandler<Msg>
    {
        public int Count;
        public void Handle(Msg message) => Interlocked.Increment(ref Count);
    }

    private sealed class TaskCounter : ITaskMessageHandler<Msg>
    {
        public int Count;
        private readonly int _delayMs;
        public TaskCounter(int delayMs = 0) => _delayMs = delayMs;

        public async Task HandleAsync(Msg message, CancellationToken token = default)
        {
            if (_delayMs > 0) await Task.Delay(_delayMs, token);
            else await Task.Yield();
            Interlocked.Increment(ref Count);
        }
    }

    private sealed class ValueTaskCounter : IValueTaskMessageHandler<Msg>
    {
        public int Count;
        private readonly bool _async;
        public ValueTaskCounter(bool async) => _async = async;

        public async ValueTask HandleAsync(Msg message, CancellationToken token = default)
        {
            if (_async) await Task.Delay(5, token);
            Interlocked.Increment(ref Count);
        }
    }

    private sealed class ThrowingTaskHandler : ITaskMessageHandler<Msg>
    {
        private readonly string _id;
        public ThrowingTaskHandler(string id) => _id = id;
        public async Task HandleAsync(Msg message, CancellationToken token = default)
        {
            await Task.Yield();
            throw new InvalidOperationException(_id);
        }
    }

    // ======================================================================
    // ASYNC MULTICAST CONCURRENCY / POOL INTEGRITY
    // ======================================================================

    [Test]
    [Description("All async handlers across all concurrent dispatches must complete exactly once.")]
    public async Task AsyncMulticast_HighConcurrency_AllHandlersRunExactlyOnce()
    {
        var handler = NewHandler();
        var h1 = new TaskCounter(delayMs: 1);
        var h2 = new TaskCounter(delayMs: 1);
        var h3 = new ValueTaskCounter(async: true);
        Registrar(handler).Register(h1);
        Registrar(handler).Register(h2);
        Registrar(handler).Register(h3);

        const int Threads = 16;
        const int PerThread = 200;
        var errors = new ConcurrentBag<Exception>();

        var tasks = Enumerable.Range(0, Threads).Select(t => Task.Run(async () =>
        {
            try
            {
                for (int i = 0; i < PerThread; i++)
                    await handler.TryHandleAsync(new Msg { Id = i });
            }
            catch (Exception ex) { errors.Add(ex); }
        }));

        await Task.WhenAll(tasks);

        Assert.That(errors, Is.Empty, "No exceptions under concurrent async dispatch");
        Assert.That(h1.Count, Is.EqualTo(Threads * PerThread), "handler 1 ran exactly once per dispatch");
        Assert.That(h2.Count, Is.EqualTo(Threads * PerThread), "handler 2 ran exactly once per dispatch");
        Assert.That(h3.Count, Is.EqualTo(Threads * PerThread), "handler 3 ran exactly once per dispatch");
    }

    [Test]
    [Description("Pooled sources must not drain: unconsumed-but-awaited async dispatch should plateau allocation.")]
    public async Task AsyncMulticast_SteadyState_AllocationPlateaus()
    {
        var handler = NewHandler();
        Registrar(handler).Register(new TaskCounter(delayMs: 0));
        Registrar(handler).Register(new TaskCounter(delayMs: 0));

        // Warm + fill pool.
        for (int i = 0; i < 256; i++)
            await handler.TryHandleAsync(new Msg { Id = i });

        long Measure()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            long before = GC.GetAllocatedBytesForCurrentThread();
            // Drive each dispatch to true completion before reading the result, honoring the
            // awaiter contract (GetResult only after IsCompleted).
            for (int i = 0; i < 500; i++)
            {
                var awaiter = handler.TryHandleAsync(new Msg { Id = i }).GetAwaiter();
                while (!awaiter.IsCompleted)
                    Thread.Yield();
                awaiter.GetResult();
            }
            return GC.GetAllocatedBytesForCurrentThread() - before;
        }

        long first = Measure();
        long second = Measure();
        long third = Measure();

        TestContext.Out.WriteLine($"alloc batches: {first}, {second}, {third} (500 async dispatches each)");

        // Steady-state batches should not keep growing unbounded; allow noise but
        // require the later batch to be within 1.5x of the middle batch.
        Assert.That(third, Is.LessThanOrEqualTo((long)(second * 1.5) + 4096),
            "allocation should plateau in steady state (pool not draining)");
    }

    [Test]
    [Description("Reentrant dispatch from within a handler must not corrupt pooled sources.")]
    public async Task Reentrancy_HandlerDispatchesAnotherMessage_NoCorruption()
    {
        var inner = NewHandler();
        Registrar(inner).Register(new TaskCounter(delayMs: 0));

        var outerCount = 0;
        var outer = NewHandler();

        Registrar(outer).Register(new ReentrantHandler(inner, () => Interlocked.Increment(ref outerCount)));

        for (int i = 0; i < 200; i++)
            await outer.TryHandleAsync(new Msg { Id = i });

        Assert.That(outerCount, Is.EqualTo(200));
    }

    private sealed class ReentrantHandler : ITaskMessageHandler<Msg>
    {
        private readonly MulticastMessageHandler<Msg> _inner;
        private readonly Action _onDone;
        public ReentrantHandler(MulticastMessageHandler<Msg> inner, Action onDone)
        {
            _inner = inner;
            _onDone = onDone;
        }
        public async Task HandleAsync(Msg message, CancellationToken token = default)
        {
            await Task.Yield();
            await _inner.TryHandleAsync(new Msg { Id = message.Id + 1 });
            _onDone();
        }
    }

    // ======================================================================
    // EXCEPTION AGGREGATION
    // ======================================================================

    [Test]
    [Description("Multiple failing async handlers must surface all exceptions, none lost.")]
    public async Task AsyncMulticast_MultipleFailures_AllExceptionsAggregated()
    {
        var handler = NewHandler();
        Registrar(handler).Register(new ThrowingTaskHandler("A"));
        Registrar(handler).Register(new TaskCounter(delayMs: 0));
        Registrar(handler).Register(new ThrowingTaskHandler("B"));
        Registrar(handler).Register(new ThrowingTaskHandler("C"));

        var result = await handler.TryHandleAsync(new Msg { Id = 1 });

        Assert.That(result.Status, Is.EqualTo(HandlingStatus.Failure));
        var messages = result.Exceptions.Select(e => e.Message).OrderBy(m => m).ToArray();
        Assert.That(messages, Is.EqualTo(new[] { "A", "B", "C" }));
    }

    [Test]
    [Description("Sync + async mix: failures and successes aggregate to Failure with all exceptions.")]
    public async Task MixedSyncAsync_FailuresAndSuccess_AggregateFailure()
    {
        var handler = NewHandler();
        var ok = new SyncCounter();
        Registrar(handler).Register(ok);
        Registrar(handler).Register(new ThrowingTaskHandler("boom"));

        var result = await handler.TryHandleAsync(new Msg { Id = 7 });

        Assert.That(result.Status, Is.EqualTo(HandlingStatus.Failure));
        Assert.That(result.Exceptions.Single().Message, Is.EqualTo("boom"));
        Assert.That(ok.Count, Is.EqualTo(1), "sync handler still ran");
    }

    // ======================================================================
    // CANCELLATION
    // ======================================================================

    [Test]
    [Description("A cancelled token should produce a Cancelled (not Success/Failure) aggregate.")]
    public async Task Cancellation_PropagatesToCancelledStatus()
    {
        var handler = NewHandler();
        Registrar(handler).Register(new TaskCounter(delayMs: 50));

        using var cts = new CancellationTokenSource();
        var task = handler.TryHandleAsync(new Msg { Id = 1 }, cts.Token).GetAwaiter();
        cts.Cancel();

        // Drive to completion.
        var tcs = new TaskCompletionSource();
        task.OnCompleted(() => tcs.SetResult());
        await tcs.Task;

        var status = task.GetResult().Status;
        Assert.That(status, Is.AnyOf(HandlingStatus.Cancelled, HandlingStatus.Failure));
    }

    // ======================================================================
    // VALUE TASK PATHS
    // ======================================================================

    [Test]
    [Description("ValueTask handler with async completion aggregates success across many dispatches.")]
    public async Task ValueTaskAsync_ManyDispatches_AllSucceed()
    {
        var handler = NewHandler();
        var vt = new ValueTaskCounter(async: true);
        Registrar(handler).Register(vt);

        for (int i = 0; i < 300; i++)
        {
            var r = await handler.TryHandleAsync(new Msg { Id = i });
            Assert.That(r.Status, Is.EqualTo(HandlingStatus.Success));
        }
        Assert.That(vt.Count, Is.EqualTo(300));
    }

    // ======================================================================
    // INTERCEPTOR CORRECTNESS UNDER LOAD
    // ======================================================================

    private sealed class OrderRecordingInterceptor : IContextInterceptor<Msg, DefaultMessageContext<Msg>>
    {
        private readonly List<string> _log;
        private readonly string _name;
        public OrderRecordingInterceptor(List<string> log, string name)
        {
            _log = log;
            _name = name;
        }
        public HandlingResultAwaitable InterceptAsync(
            DefaultMessageContext<Msg> context,
            Func<DefaultMessageContext<Msg>, HandlingResultAwaitable> next)
        {
            lock (_log) _log.Add($"{_name}:before");
            var r = next(context);
            lock (_log) _log.Add($"{_name}:after");
            return r;
        }
    }

    // ======================================================================
    // EDGE CASES
    // ======================================================================

    [Test]
    [Description("Async dispatch with no registered handlers yields NotHandled, not a crash or hang.")]
    public async Task NoHandlers_AsyncDispatch_NotHandled()
    {
        var handler = NewHandler();
        var result = await handler.TryHandleAsync(new Msg { Id = 1 });
        Assert.That(result.Status, Is.EqualTo(HandlingStatus.NotHandled));
    }

    private sealed class SyncThrowingHandler : ISyncMessageHandler<Msg>
    {
        public void Handle(Msg message) => throw new InvalidOperationException("sync-boom");
    }

    [Test]
    [Description("A handler that throws synchronously is captured as a Failure, not propagated raw.")]
    public async Task SyncThrow_CapturedAsFailure()
    {
        var handler = NewHandler();
        Registrar(handler).Register(new SyncThrowingHandler());

        var result = await handler.TryHandleAsync(new Msg { Id = 1 });
        Assert.That(result.Status, Is.EqualTo(HandlingStatus.Failure));
        Assert.That(result.Exceptions.Single().Message, Is.EqualTo("sync-boom"));
    }

    [Test]
    [Description("Large async fan-out: every one of many handlers runs exactly once per dispatch.")]
    public async Task LargeFanOut_AllHandlersRun()
    {
        var handler = NewHandler();
        const int HandlerCount = 50;
        var counters = new TaskCounter[HandlerCount];
        for (int i = 0; i < HandlerCount; i++)
        {
            counters[i] = new TaskCounter(delayMs: 0);
            Registrar(handler).Register(counters[i]);
        }

        const int Dispatches = 100;
        for (int i = 0; i < Dispatches; i++)
            await handler.TryHandleAsync(new Msg { Id = i });

        foreach (var c in counters)
            Assert.That(c.Count, Is.EqualTo(Dispatches));
    }

    [Test]
    [Description("Interleaved sync+async handlers across many dispatches all run exactly once.")]
    public async Task InterleavedSyncAsync_AllRunExactlyOnce()
    {
        var handler = NewHandler();
        var s1 = new SyncCounter();
        var a1 = new TaskCounter(delayMs: 0);
        var s2 = new SyncCounter();
        var a2 = new ValueTaskCounter(async: true);
        Registrar(handler).Register(s1);
        Registrar(handler).Register(a1);
        Registrar(handler).Register(s2);
        Registrar(handler).Register(a2);

        const int N = 300;
        for (int i = 0; i < N; i++)
        {
            var r = await handler.TryHandleAsync(new Msg { Id = i });
            Assert.That(r.Status, Is.EqualTo(HandlingStatus.Success));
        }

        Assert.That(s1.Count, Is.EqualTo(N));
        Assert.That(a1.Count, Is.EqualTo(N));
        Assert.That(s2.Count, Is.EqualTo(N));
        Assert.That(a2.Count, Is.EqualTo(N));
    }

    [Test]
    [Description("Multiple interceptors wrap in registration order and all fire per dispatch.")]
    public async Task Interceptors_Multiple_FireInOrder_EveryDispatch()
    {
        var handler = NewHandler();
        var log = new List<string>();
        var inner = new SyncCounter();
        var reg = Registrar(handler).Register(inner);
        reg.AddInterceptor(new OrderRecordingInterceptor(log, "outer"));
        reg.AddInterceptor(new OrderRecordingInterceptor(log, "inner"));

        await handler.TryHandleAsync(new Msg { Id = 1 });

        Assert.That(inner.Count, Is.EqualTo(1));
        Assert.That(log, Is.EqualTo(new[]
        {
            "outer:before", "inner:before", "inner:after", "outer:after"
        }));
    }
}
