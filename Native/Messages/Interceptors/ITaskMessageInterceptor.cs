using Chopsticks.Messages.Handlers.Sources;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Chopsticks.Messages.Interceptors
{
    public interface ITaskMessageInterceptor<TMessage> : IMessageInterceptor<TMessage>
    {
        HandlingAwaitable IMessageInterceptor<TMessage>.InterceptAsync(
            TMessage message, CancellationToken token, 
            Func<TMessage, CancellationToken, HandlingAwaitable> next)
        {
            var source = new TaskHandlingPromiseSource();  // TODO :: Rent from a pool.
            source.Init(InterceptAsync(message, token, next).GetAwaiter());

            return new HandlingAwaitable(source);
        }

        new Task InterceptAsync(TMessage message, CancellationToken token, 
            Func<TMessage, CancellationToken, HandlingAwaitable> next);
    }
}
