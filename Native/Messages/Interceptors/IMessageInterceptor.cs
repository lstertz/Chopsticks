using System;
using System.Threading;

namespace Chopsticks.Messages.Interceptors
{
    public interface IMessageInterceptor<TMessage>
    {
        HandlingResultAwaitable InterceptAsync(TMessage message, CancellationToken token,
            Func<HandlingResultAwaitable> next);
    }
}
