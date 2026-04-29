using Chopsticks.Messages.Handlers.Sources;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Chopsticks.Messages.Interceptors
{
    public interface ITaskContractInterceptor<TContract> : 
        IContractInterceptor<TContract>
    {
        new Task InterceptAsync<TActualContext>(TActualContext context,
            CancellationToken token, Func<TActualContext, HandlingResultAwaitable> next)
            where TActualContext : TContract;

        HandlingResultAwaitable IContractInterceptor<TContract>.InterceptAsync<TContractedContext>(
            TContractedContext context,
            CancellationToken token, 
            Func<TContractedContext, HandlingResultAwaitable> next)
        {
            var source = TryHandleAsyncTaskPromiseSource.Pool.Rent();
            source.Init((this as ITaskContractInterceptor<TContract>)
                .InterceptAsync(context, token, next).GetAwaiter());

            return new HandlingResultAwaitable(source);
        }
    }
}
