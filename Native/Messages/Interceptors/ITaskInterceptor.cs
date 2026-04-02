using Chopsticks.Messages.Handlers.Sources;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Chopsticks.Messages.Interceptors
{
    public interface ITaskInterceptor : IInterceptor
    {
        new Task InterceptAsync(CancellationToken token,
            Func<HandlingAwaitable> next);

        HandlingAwaitable IInterceptor.InterceptAsync(CancellationToken token,
            Func<HandlingAwaitable> next)
        {
            var source = new TryHandleAsyncPromiseSource();  // TODO :: Rent from a pool.
            source.Init((this as ITaskInterceptor).InterceptAsync(token, next).GetAwaiter());

            return new HandlingAwaitable(source);
        }
    }
}
