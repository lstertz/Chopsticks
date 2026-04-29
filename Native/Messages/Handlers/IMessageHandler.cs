using System;
using System.Threading;
using Chopsticks.Messages.Handlers.Sources;

namespace Chopsticks.Messages.Handlers
{
    public interface IMessageHandler<TMessage>
    {
        HandlingCompletionPromise Handle(TMessage message,
            SynchronizationContext? asyncContext = null)
        {
            var awaitable = TryHandle(message);
            var source = new HandlePromiseSource();
            source.Init(awaitable.Source);

            return new HandlingCompletionPromise(source, 
                asyncContext ?? SynchronizationContext.Current);
        }

        HandlingCompletionAwaitable HandleAsync(TMessage message, CancellationToken token = default)
        {
            var awaitable = TryHandleAsync(message, token);
            var source = new HandleAsyncPromiseSource();
            source.Init(awaitable.Source);

            return new HandlingCompletionAwaitable(source);
        }

        HandlingResultPromise TryHandle(TMessage message);

        HandlingResultAwaitable TryHandleAsync(TMessage message, CancellationToken token = default);
    }
}
