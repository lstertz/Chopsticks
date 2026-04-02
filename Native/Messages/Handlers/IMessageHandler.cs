using System;
using System.Threading;
using Chopsticks.Messages.Handlers.Sources;

namespace Chopsticks.Messages.Handlers
{
    public interface IMessageHandler<TMessage>
    {
        // TODO :: Re-evaluate the return type or completion action, since it 
        // may be desirable to know whether the handling was handled or not.

        void Handle(TMessage message, Action onCompletion, 
            SynchronizationContext? asyncContext = null)
        {
            var awaitable = TryHandle(message);
            var source = new HandlePromiseSource();
            source.Init(awaitable.Source);

            onCompletion?.Invoke();
        }

        // TODO :: Re-evaluate the return type.
        HandlingAwaitable HandleAsync(TMessage message, CancellationToken token = default)
        {
            var awaitable = TryHandleAsync(message, token);
            var source = new HandleAsyncPromiseSource();
            source.Init(awaitable.Source);

            return new HandlingAwaitable(source);
        }

        HandlingPromise TryHandle(TMessage message);

        HandlingAwaitable TryHandleAsync(TMessage message, CancellationToken token = default);
    }
}
