using Chopsticks.Messages;
using Chopsticks.Messages.Handlers;
using Chopsticks.Messages.Handlers.Multicast;
using Chopsticks.Messages.Interceptors;
using Chopsticks.Messages.Registration;
using NUnit.Framework;
using System;
using System.Threading;

namespace Chopsticks.Tests;

/// <summary>
/// Quantifies per-dispatch allocation of the interceptor pipeline so optimization
/// effort can be aimed at the path that actually allocates. All handlers and
/// interceptors here are fully synchronous, so each dispatch completes inline and
/// the only heap traffic is the pipeline's own per-dispatch bookkeeping.
/// </summary>
[TestFixture]
[Category("Hardening")]
public class InterceptorAllocationTests
{
    public sealed class Msg
    {
        public int Id { get; init; }
    }

    private static MulticastMessageHandler<Msg> NewHandler() => new();

    private static IMessageHandlerRegistrar<Msg, DefaultMessageContext<Msg>> Registrar(
        MulticastMessageHandler<Msg> handler) =>
        (IMessageHandlerRegistrar<Msg, DefaultMessageContext<Msg>>)handler;

    private sealed class SyncNoop : ISyncMessageHandler<Msg>
    {
        public int Count;
        public void Handle(Msg message) => Count++;
    }

    private sealed class PassThroughContextInterceptor :
        IContextInterceptor<Msg, DefaultMessageContext<Msg>>
    {
        public HandlingResultAwaitable InterceptAsync(
            DefaultMessageContext<Msg> context,
            Func<DefaultMessageContext<Msg>, HandlingResultAwaitable> next) =>
            next(context);
    }

    private sealed class PassThroughInterceptor : IInterceptor
    {
        public HandlingResultAwaitable InterceptAsync(
            CancellationToken token,
            Func<HandlingResultAwaitable> next) =>
            next();
    }

    private static long MeasurePerDispatch(MulticastMessageHandler<Msg> handler, int iterations)
    {
        // Warm the path (JIT, any one-time caches).
        for (int i = 0; i < 1000; i++)
            DriveSync(handler, i);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < iterations; i++)
            DriveSync(handler, i);
        long total = GC.GetAllocatedBytesForCurrentThread() - before;

        return total / iterations;
    }

    private static void DriveSync(MulticastMessageHandler<Msg> handler, int id)
    {
        var awaiter = handler.TryHandleAsync(new Msg { Id = id }).GetAwaiter();
        // Fully synchronous path: must already be complete.
        if (!awaiter.IsCompleted)
            throw new InvalidOperationException("expected synchronous completion");
        awaiter.GetResult();
    }

    [Test]
    [Description("Baseline (no interceptor), IContextInterceptor, and IInterceptor-adapter per-dispatch allocation.")]
    public void Interceptor_PerDispatchAllocation_Quantified()
    {
        const int Iterations = 20000;

        var baseHandler = NewHandler();
        Registrar(baseHandler).Register(new SyncNoop());
        long baseline = MeasurePerDispatch(baseHandler, Iterations);

        var ctxHandler = NewHandler();
        var ctxReg = Registrar(ctxHandler).Register(new SyncNoop());
        ctxReg.AddInterceptor(new PassThroughContextInterceptor());
        long contextInterceptor = MeasurePerDispatch(ctxHandler, Iterations);

        var adapterHandler = NewHandler();
        var adapterReg = Registrar(adapterHandler).Register(new SyncNoop());
        adapterReg.AddInterceptor(new PassThroughInterceptor());
        long adapterInterceptor = MeasurePerDispatch(adapterHandler, Iterations);

        TestContext.Out.WriteLine($"baseline (no interceptor):        {baseline} B/dispatch");
        TestContext.Out.WriteLine($"IContextInterceptor (no adapter): {contextInterceptor} B/dispatch");
        TestContext.Out.WriteLine($"IInterceptor (adapter):           {adapterInterceptor} B/dispatch");
        TestContext.Out.WriteLine($"adapter overhead vs context:      {adapterInterceptor - contextInterceptor} B/dispatch");

        // The recommended path (IContextInterceptor) must not add meaningful per-dispatch
        // allocation over the no-interceptor baseline: the pipeline closures are built once.
        Assert.That(contextInterceptor - baseline, Is.LessThanOrEqualTo(8),
            "IContextInterceptor path should be allocation-free per dispatch");
    }
}
