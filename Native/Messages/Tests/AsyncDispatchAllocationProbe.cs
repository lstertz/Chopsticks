using Chopsticks.Messages;
using Chopsticks.Messages.Handlers.Sources;
using Chopsticks.Messages.Registration;
using NUnit.Framework;
using System;

namespace Chopsticks.Tests;

/// <summary>
/// Regression guard on the allocation footprint of <see cref="SequentialHandlingPromiseSource{TMessage,TContext}"/>.
/// This source is allocated fresh per async-fallback dispatch (pooling is intentionally disabled
/// for it — see the type's remarks), so its size is the per-dispatch cost on that path. The guard
/// keeps that footprint from silently growing. Measured on a single thread, so the per-thread
/// allocation counter is reliable here (no <c>await</c> thread hops).
/// </summary>
[TestFixture]
[Category("Hardening")]
public class AsyncDispatchAllocationProbe
{
    public sealed class Msg { public int Id { get; init; } }

    [Test]
    [Description("SequentialHandlingPromiseSource rent footprint (object + ctor sub-allocations) stays bounded.")]
    public void SequentialSource_RentFootprint_Bounded()
    {
        // Warm (JIT).
        SequentialHandlingPromiseSource<Msg, DefaultMessageContext<Msg>>? warm = null;
        for (int i = 0; i < 1000; i++)
            warm = SequentialHandlingPromiseSource<Msg, DefaultMessageContext<Msg>>.Rent();
        GC.KeepAlive(warm);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        const int Iterations = 100000;
        long before = GC.GetAllocatedBytesForCurrentThread();
        SequentialHandlingPromiseSource<Msg, DefaultMessageContext<Msg>>? keep = null;
        for (int i = 0; i < Iterations; i++)
            keep = SequentialHandlingPromiseSource<Msg, DefaultMessageContext<Msg>>.Rent();
        long perRent = (GC.GetAllocatedBytesForCurrentThread() - before) / Iterations;
        GC.KeepAlive(keep);

        TestContext.Out.WriteLine($"SequentialHandlingPromiseSource rent footprint: {perRent} B");

        // Current footprint is ~360 B (instance + InitiateDefaultContinuations delegate +
        // _onHandlerCompletion delegate + _gate object). Guard against regressions/growth.
        Assert.That(perRent, Is.LessThanOrEqualTo(512),
            "per-rent footprint should stay bounded (watch for added per-instance allocations)");
    }
}
