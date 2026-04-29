using System;
using System.Threading;

namespace Chopsticks.Messages.Interceptors
{
    public interface IContractInterceptor<TContract>
    {
        HandlingResultAwaitable InterceptAsync<TContractedContext>(TContractedContext context,
            CancellationToken token, Func<TContractedContext, HandlingResultAwaitable> next)
            where TContractedContext : TContract;
    }
}
