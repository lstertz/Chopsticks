using Chopsticks.Messages.Handlers;
using System;
using System.Threading;

namespace Chopsticks.Messages.Interceptors
{
    public interface IMessageInterceptor<TMessage>
    {
        HandlingAwaitable InterceptAsync(TMessage message, CancellationToken token, 
            Func<TMessage, CancellationToken, HandlingAwaitable> next);
    }
}
