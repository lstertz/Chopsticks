using System.Threading;
using Chopsticks.Messages.Handlers.Sources;

namespace Chopsticks.Messages.Handlers;

public interface IContextHandler<TMessage, TContext> : IMessageHandler<TMessage>
    where TContext : IMessageContext<TMessage>, new()
{
    HandlingCompletionPromise Handle(TContext context,
        SynchronizationContext? asyncContext = null)
    {
        var awaitable = TryHandle(context);
        var source = new HandlePromiseSource();
        source.Init(awaitable.Source);

        return new HandlingCompletionPromise(source,
            asyncContext ?? SynchronizationContext.Current);
    }

    HandlingCompletionAwaitable HandleAsync(TContext context)
    {
        var awaitable = TryHandleAsync(context);
        var source = new HandleAsyncPromiseSource();
        source.Init(awaitable.Source);

        return new HandlingCompletionAwaitable(source);
    }

    HandlingResultPromise IMessageHandler<TMessage>.TryHandle(TMessage message) =>
        TryHandle(new TContext()
        {
            CancellationToken = default,
            Message = message
        });

    HandlingResultPromise TryHandle(TContext context);


    HandlingResultAwaitable IMessageHandler<TMessage>.TryHandleAsync(
        TMessage message, CancellationToken token) =>

        TryHandleAsync(new TContext()
        {
            CancellationToken = token,
            Message = message
        });

    HandlingResultAwaitable TryHandleAsync(TContext context);
}