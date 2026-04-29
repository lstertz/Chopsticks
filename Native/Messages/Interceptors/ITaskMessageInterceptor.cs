using Chopsticks.Messages.Handlers.Sources;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Chopsticks.Messages.Interceptors
{
    public interface ITaskMessageInterceptor<TMessage> : IMessageInterceptor<TMessage>
    {
        new Task InterceptAsync(TMessage message, CancellationToken token,
            Func<HandlingResultAwaitable> next);

        HandlingResultAwaitable IMessageInterceptor<TMessage>.InterceptAsync(TMessage message, 
            CancellationToken token,
            Func<HandlingResultAwaitable> next)
        {
            var source = new TryHandleAsyncPromiseSource();  // TODO :: Rent from a pool.
            source.Init((this as ITaskMessageInterceptor<TMessage>).InterceptAsync(message, token, next).GetAwaiter());

            return new HandlingResultAwaitable(source);
        }
    }
}
