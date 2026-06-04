using Chopsticks.Messages;
using Chopsticks.Messages.Handlers;
using Chopsticks.Messages.Handlers.Multicast;
using Chopsticks.Messages.Registration;
using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Chopsticks.Tests;

/// <summary>
/// Stresses the async-fallback dispatch path (a sync handler followed by an async handler, so the
/// sync fast path bails to a pooled <c>SequentialHandlingPromiseSource</c>) under heavy concurrency.
/// Guards the re-enabled pooling: torn-read/recycle corruption would surface here as a crash, a
/// hang, or a lost handler run.
/// </summary>
[TestFixture]
[Category("Hardening")]
public class FallbackConcurrencyTests
{
    public sealed class Msg { public int Id { get; init; } }

    private static MulticastMessageHandler<Msg> NewHandler() => new();
    private static IMessageHandlerRegistrar<Msg, DefaultMessageContext<Msg>> Registrar(
        MulticastMessageHandler<Msg> h) =>
        (IMessageHandlerRegistrar<Msg, DefaultMessageContext<Msg>>)h;

    private sealed class SyncCounter : ISyncMessageHandler<Msg>
    {
        public int Count;
        public void Handle(Msg m) => Interlocked.Increment(ref Count);
    }

    private sealed class YieldCounter : IValueTaskMessageHandler<Msg>
    {
        public int Count;
        public async ValueTask HandleAsync(Msg m, CancellationToken token = default)
        {
            await Task.Yield();
            Interlocked.Increment(ref Count);
        }
    }

    [Test]
    [Timeout(120000)]
    [Description("Heavy concurrent async-fallback dispatch (await): no crash, no hang, no lost runs.")]
    public async Task Fallback_HighConcurrency_Await_Stable()
    {
        var handler = NewHandler();
        var sync = new SyncCounter();
        var a = new YieldCounter();
        var b = new YieldCounter();
        Registrar(handler).Register(sync);    // sync first -> fast path starts
        Registrar(handler).Register(a);        // async -> forces fallback to a pooled source
        Registrar(handler).Register(b);

        const int Threads = 32;
        const int PerThread = 500;
        var errors = new ConcurrentBag<Exception>();

        var tasks = Enumerable.Range(0, Threads).Select(_ => Task.Run(async () =>
        {
            try
            {
                for (int i = 0; i < PerThread; i++)
                {
                    var r = await handler.TryHandleAsync(new Msg { Id = i });
                    if (r.Status != HandlingStatus.Success)
                        throw new Exception($"unexpected status {r.Status}");
                }
            }
            catch (Exception ex) { errors.Add(ex); }
        }));

        await Task.WhenAll(tasks);

        Assert.That(errors, Is.Empty, "no exceptions/crashes under concurrent fallback dispatch");
        int expected = Threads * PerThread;
        Assert.That(sync.Count, Is.EqualTo(expected), "sync handler ran exactly once per dispatch");
        Assert.That(a.Count, Is.EqualTo(expected), "async A ran exactly once per dispatch");
        Assert.That(b.Count, Is.EqualTo(expected), "async B ran exactly once per dispatch");
    }

    [Test]
    [Timeout(120000)]
    [Description("Concurrent async-fallback dispatch consumed via ToPromise().OnCompletion: every callback fires.")]
    public async Task Fallback_HighConcurrency_ToPromise_AllCallbacksFire()
    {
        var handler = NewHandler();
        var sync = new SyncCounter();
        var a = new YieldCounter();
        Registrar(handler).Register(sync);
        Registrar(handler).Register(a);

        const int Threads = 16;
        const int PerThread = 500;
        var errors = new ConcurrentBag<Exception>();

        var tasks = Enumerable.Range(0, Threads).Select(_ => Task.Run(async () =>
        {
            try
            {
                for (int i = 0; i < PerThread; i++)
                {
                    var tcs = new TaskCompletionSource();
                    handler.TryHandleAsync(new Msg { Id = i })
                        .ToPromise()
                        .OnCompletion(_ => tcs.TrySetResult());
                    var done = await Task.WhenAny(tcs.Task, Task.Delay(5000));
                    if (done != tcs.Task)
                        throw new Exception("OnCompletion callback dropped");
                }
            }
            catch (Exception ex) { errors.Add(ex); }
        }));

        await Task.WhenAll(tasks);

        Assert.That(errors, Is.Empty, "no dropped callbacks / no exceptions");
        int expected = Threads * PerThread;
        Assert.That(sync.Count, Is.EqualTo(expected));
        Assert.That(a.Count, Is.EqualTo(expected));
    }

    [Test]
    [Timeout(120000)]
    [Description("Reentrant async-fallback dispatch (nested pooled multicasters) under concurrency.")]
    public async Task Reentrant_Fallback_Concurrency_NoCorruption()
    {
        var inner = NewHandler();
        var innerSync = new SyncCounter();
        var innerAsync = new YieldCounter();
        Registrar(inner).Register(innerSync);
        Registrar(inner).Register(innerAsync);

        var outer = NewHandler();
        var outerCount = 0;
        Registrar(outer).Register(new ReentrantHandler(inner, () => Interlocked.Increment(ref outerCount)));

        const int Threads = 16;
        const int PerThread = 400;
        var errors = new ConcurrentBag<Exception>();

        var tasks = Enumerable.Range(0, Threads).Select(_ => Task.Run(async () =>
        {
            try
            {
                for (int i = 0; i < PerThread; i++)
                    await outer.TryHandleAsync(new Msg { Id = i });
            }
            catch (Exception ex) { errors.Add(ex); }
        }));

        await Task.WhenAll(tasks);

        Assert.That(errors, Is.Empty);
        Assert.That(outerCount, Is.EqualTo(Threads * PerThread));
        Assert.That(innerSync.Count, Is.EqualTo(Threads * PerThread));
        Assert.That(innerAsync.Count, Is.EqualTo(Threads * PerThread));
    }

    [Test]
    [Timeout(120000)]
    [Description("Await consumers (pooled) and ToPromise consumers (pooling-suppressed) interleaved on the same handler: the ownership boundary must hold — no crash, no dropped callback, no lost handler run.")]
    public async Task Fallback_Mixed_AwaitAndToPromise_Concurrent_NoCorruption()
    {
        var handler = NewHandler();
        var sync = new SyncCounter();
        var a = new YieldCounter();
        Registrar(handler).Register(sync);
        Registrar(handler).Register(a);

        const int Threads = 32;
        const int PerThread = 500;
        var errors = new ConcurrentBag<Exception>();
        int awaited = 0, promised = 0;

        var tasks = Enumerable.Range(0, Threads).Select(t => Task.Run(async () =>
        {
            try
            {
                for (int i = 0; i < PerThread; i++)
                {
                    // Alternate the two consumption styles so pooled (await) and suppressed
                    // (ToPromise) dispatches contend for the same static pool simultaneously.
                    if (((t + i) & 1) == 0)
                    {
                        var r = await handler.TryHandleAsync(new Msg { Id = i });
                        if (r.Status != HandlingStatus.Success)
                            throw new Exception($"await unexpected status {r.Status}");
                        Interlocked.Increment(ref awaited);
                    }
                    else
                    {
                        var tcs = new TaskCompletionSource();
                        handler.TryHandleAsync(new Msg { Id = i })
                            .ToPromise()
                            .OnCompletion(_ => tcs.TrySetResult());
                        var done = await Task.WhenAny(tcs.Task, Task.Delay(5000));
                        if (done != tcs.Task)
                            throw new Exception("ToPromise OnCompletion callback dropped");
                        Interlocked.Increment(ref promised);
                    }
                }
            }
            catch (Exception ex) { errors.Add(ex); }
        }));

        await Task.WhenAll(tasks);

        Assert.That(errors, Is.Empty, "ownership boundary holds across pooled + suppressed consumers");
        int expected = Threads * PerThread;
        Assert.That(awaited + promised, Is.EqualTo(expected), "every dispatch was consumed exactly once");
        Assert.That(sync.Count, Is.EqualTo(expected), "sync handler ran exactly once per dispatch");
        Assert.That(a.Count, Is.EqualTo(expected), "async handler ran exactly once per dispatch");
    }

    private sealed class ReentrantHandler : IValueTaskMessageHandler<Msg>
    {
        private readonly MulticastMessageHandler<Msg> _inner;
        private readonly Action _onDone;
        public ReentrantHandler(MulticastMessageHandler<Msg> inner, Action onDone)
        {
            _inner = inner;
            _onDone = onDone;
        }
        public async ValueTask HandleAsync(Msg m, CancellationToken token = default)
        {
            await Task.Yield();
            await _inner.TryHandleAsync(new Msg { Id = m.Id + 1 });
            _onDone();
        }
    }
}
