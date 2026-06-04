using Chopsticks.Messages;
using Chopsticks.Messages.Handlers.Sources;
using Chopsticks.Messages.Registration;
using NUnit.Framework;
using System;

namespace Chopsticks.Tests;

/// <summary>
/// Verifies that <see cref="SequentialHandlingPromiseSource{TMessage,TContext}"/> pooling reclaims
/// instances on the single-terminal (await) path: rent + return in a loop should allocate ~nothing
/// once the pool is warm. The async-fallback dispatch path rents this source, so a healthy pool
/// keeps that path allocation-light. (The fluent ToPromise path intentionally suppresses pooling —
/// see the type header — and is covered by FallbackConcurrencyTests.)
/// </summary>
[TestFixture]
[Category("Hardening")]
public class AsyncDispatchAllocationProbe
{
    public sealed class Msg { public int Id { get; init; } }

    [Test]
    [Description("Renting then returning in a loop allocates ~0 once the pool is warm (pooling reclaims).")]
    public void SequentialSource_RentReturn_Reclaims()
    {
        // Warm the pool: rent + dispose so instances are recycled.
        for (int i = 0; i < 2000; i++)
        {
            var s = SequentialHandlingPromiseSource<Msg, DefaultMessageContext<Msg>>.Rent();
            s.Dispose();
        }

        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        const int Iterations = 100000;
        long before = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < Iterations; i++)
        {
            var s = SequentialHandlingPromiseSource<Msg, DefaultMessageContext<Msg>>.Rent();
            s.Dispose();
        }
        long perOp = (GC.GetAllocatedBytesForCurrentThread() - before) / Iterations;

        TestContext.Out.WriteLine($"warm rent+return: {perOp} B/op");

        // With a warm pool, rent+return should be allocation-free (small slack for measurement noise).
        Assert.That(perOp, Is.LessThanOrEqualTo(16),
            "pooling should reclaim instances on the await/terminal path (warm rent+return ~0 B/op)");
    }

    [Test]
    [Description("A source handed to a fluent promise suppresses pooling (not returned to the pool).")]
    public void SuppressPooling_PreventsReturn()
    {
        // Warm + drain so the pool is empty and counted from a known state.
        var warm = SequentialHandlingPromiseSource<Msg, DefaultMessageContext<Msg>>.Rent();
        warm.Dispose(); // returns to pool

        // Rent it back, suppress pooling (as the fluent promise ctor does), then dispose.
        var s = SequentialHandlingPromiseSource<Msg, DefaultMessageContext<Msg>>.Rent();
        ((IHandlingPromiseSource)s).SuppressPooling();
        s.Dispose();

        // The suppressed instance must NOT have been returned: the next rent must be a different
        // object (the pool is now empty, so Rent constructs fresh).
        var next = SequentialHandlingPromiseSource<Msg, DefaultMessageContext<Msg>>.Rent();
        Assert.That(ReferenceEquals(next, s), Is.False,
            "a pooling-suppressed source must not be returned to the pool");
    }
}
