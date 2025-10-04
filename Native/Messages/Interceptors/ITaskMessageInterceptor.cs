using Chopsticks.Messages.Handlers.Sources;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Chopsticks.Messages.Interceptors
{
    public interface ITaskMessageInterceptor<TMessage> : IMessageInterceptor<TMessage>
    {
        new Task InterceptAsync(TMessage message, CancellationToken token,
            Func<HandlingAwaitable> next);

        HandlingAwaitable IMessageInterceptor<TMessage>.InterceptAsync(TMessage message, 
            CancellationToken token,
            Func<HandlingAwaitable> next)
        {
            var source = new TaskHandlingPromiseSource();  // TODO :: Rent from a pool.
            source.Init((this as ITaskMessageInterceptor<TMessage>).InterceptAsync(message, token, next).GetAwaiter());

            return new HandlingAwaitable(source);
        }
    }
}
