using System;
using System.Threading;

namespace Chopsticks.Messages.Interceptors
{

    public interface IInterceptor
    {
        HandlingResultAwaitable InterceptAsync(CancellationToken token, 
            Func<HandlingResultAwaitable> next);
    }
}
