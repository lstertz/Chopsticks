using Chopsticks.Messages;
using Chopsticks.Messages.Handlers;
using Chopsticks.Messages.Handlers.Multicast;
using Chopsticks.Messages.Registration;
using NUnit.Framework;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Chopsticks.Tests;

/// <summary>
/// Guards against the HandlingResultPromise callback-registration race: when an async source
/// completes (on a pool thread) concurrently with a fluent OnXxx registration, the callback must
/// still fire exactly once. Before the fix, the check-then-register in the fluent methods could
/// lose the callback against the completion path, hanging anyone awaiting it.
/// </summary>
[TestFixture]
[Category("Hardening")]
public class PromiseCallbackRaceTests
{
    public sealed class Msg { public int Id { get; init; } }

    private sealed class YieldHandler : IValueTaskMessageHandler<Msg>
    {
        public async ValueTask HandleAsync(Msg m, CancellationToken token = default)
        {
            await Task.Yield();
        }
    }

    private sealed class YieldThenThrow : IValueTaskMessageHandler<Msg>
    {
        public async ValueTask HandleAsync(Msg m, CancellationToken token = default)
        {
            await Task.Yield();
            throw new InvalidOperationException("boom");
        }
    }

    [Test]
    [Timeout(60000)]
    [Description("OnCompletion fires on every async dispatch with no registration/completion race drop.")]
    public async Task OnCompletion_NeverDropped_UnderAsyncCompletion()
    {
        var handler = new MulticastMessageHandler<Msg>();
        ((IMessageHandlerRegistrar<Msg, DefaultMessageContext<Msg>>)handler).Register(new YieldHandler());

        const int Iterations = 50000;
        for (int i = 0; i < Iterations; i++)
        {
            var tcs = new TaskCompletionSource();
            handler.TryHandleAsync(new Msg { Id = i })
                .ToPromise()
                .OnCompletion(_ => tcs.TrySetResult());

            var done = await Task.WhenAny(tcs.Task, Task.Delay(2000));
            Assert.That(done, Is.SameAs(tcs.Task), $"OnCompletion callback dropped at iteration {i}");
        }
    }

    [Test]
    [Timeout(60000)]
    [Description("OnSuccess and OnFailure both fire reliably under concurrent async completion.")]
    public async Task OnSuccessAndOnFailure_NeverDropped()
    {
        var ok = new MulticastMessageHandler<Msg>();
        ((IMessageHandlerRegistrar<Msg, DefaultMessageContext<Msg>>)ok).Register(new YieldHandler());

        var bad = new MulticastMessageHandler<Msg>();
        ((IMessageHandlerRegistrar<Msg, DefaultMessageContext<Msg>>)bad).Register(new YieldThenThrow());

        const int Iterations = 20000;
        for (int i = 0; i < Iterations; i++)
        {
            var successTcs = new TaskCompletionSource();
            ok.TryHandleAsync(new Msg { Id = i }).ToPromise().OnSuccess(() => successTcs.TrySetResult());
            Assert.That(await Task.WhenAny(successTcs.Task, Task.Delay(2000)), Is.SameAs(successTcs.Task),
                $"OnSuccess dropped at iteration {i}");

            var failTcs = new TaskCompletionSource();
            bad.TryHandleAsync(new Msg { Id = i }).ToPromise().OnFailure(_ => failTcs.TrySetResult());
            Assert.That(await Task.WhenAny(failTcs.Task, Task.Delay(2000)), Is.SameAs(failTcs.Task),
                $"OnFailure dropped at iteration {i}");
        }
    }
}
