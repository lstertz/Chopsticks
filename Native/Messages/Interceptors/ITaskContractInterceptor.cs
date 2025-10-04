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
            CancellationToken token, Func<TActualContext, HandlingAwaitable> next)
            where TActualContext : TContract;

        HandlingAwaitable IContractInterceptor<TContract>.InterceptAsync<TContractedContext>(
            TContractedContext context,
            CancellationToken token, 
            Func<TContractedContext, HandlingAwaitable> next)
        {
            var source = new TaskHandlingPromiseSource();  // TODO :: Rent from a pool.
            source.Init((this as ITaskContractInterceptor<TContract>)
                .InterceptAsync(context, token, next).GetAwaiter());

            return new HandlingAwaitable(source);
        }
    }
}
