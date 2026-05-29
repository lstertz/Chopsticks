using Chopsticks.Messages;
using Chopsticks.Messages.Handlers.Sources;
using Chopsticks.Messages.Registration;
using NUnit.Framework;
using System;

namespace Chopsticks.Tests;

/// <summary>
/// Bounds the per-instance footprint of <see cref="SequentialHandlingPromiseSource{TMessage,TContext}"/>,
/// which is currently allocated fresh per async-fallback dispatch (pooling for this source is
/// intentionally disabled — see the type's header comment for the use-after-recycle rationale).
/// This guard fails loudly if the instance footprint silently grows, since that cost is paid on
/// every async-fallback dispatch.
/// </summary>
[TestFixture]
[Category("Hardening")]
public class AsyncDispatchAllocationProbe
{
    public sealed class Msg { public int Id { get; init; } }

    [Test]
    [Description("Per-instance Rent() footprint stays bounded (regression guard on the async-fallback cost).")]
    public void SequentialSource_RentFootprint_Bounded()
    {
        // Warm JIT / type init.
        for (int i = 0; i < 1000; i++)
            _ = SequentialHandlingPromiseSource<Msg, DefaultMessageContext<Msg>>.Rent();

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        const int Iterations = 50000;
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < Iterations; i++)
            _ = SequentialHandlingPromiseSource<Msg, DefaultMessageContext<Msg>>.Rent();
        long perInstance = (GC.GetAllocatedBytesForCurrentThread() - before) / Iterations;

        TestContext.Out.WriteLine($"SequentialHandlingPromiseSource rent footprint: {perInstance} B/instance");

        // Instance + ctor sub-allocations measured at ~360 B. Bound with slack; fail if it grows.
        Assert.That(perInstance, Is.LessThanOrEqualTo(512),
            "SequentialHandlingPromiseSource per-instance footprint should stay bounded");
    }
}
