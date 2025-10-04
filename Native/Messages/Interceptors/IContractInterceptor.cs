using System;
using System.Threading;

namespace Chopsticks.Messages.Interceptors
{
    public interface IContractInterceptor<TContract>
    {
        HandlingAwaitable InterceptAsync<TContractedContext>(TContractedContext context,
            CancellationToken token, Func<TContractedContext, HandlingAwaitable> next)
            where TContractedContext : TContract;
    }
}
