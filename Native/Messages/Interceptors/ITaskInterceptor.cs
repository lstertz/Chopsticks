using Chopsticks.Messages.Handlers.Sources;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Chopsticks.Messages.Interceptors
{
    public interface ITaskInterceptor : IInterceptor
    {
        new Task InterceptAsync(CancellationToken token,
            Func<HandlingResultAwaitable> next);

        HandlingResultAwaitable IInterceptor.InterceptAsync(CancellationToken token,
            Func<HandlingResultAwaitable> next)
        {
            var source = TryHandleAsyncPromiseSource.Rent();
            source.Init((this as ITaskInterceptor).InterceptAsync(token, next).GetAwaiter());

            return new HandlingResultAwaitable(source);
        }
    }
}
