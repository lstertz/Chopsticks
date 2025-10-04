using Chopsticks.Messages.Handlers;
using System;

namespace Chopsticks.Messages.Interceptors
{
    public interface IContextInterceptor<TMessage, TContext>
        where TContext : IMessageContext<TMessage>
    {
        HandlingAwaitable InterceptAsync(TContext context, 
            Func<TContext, HandlingAwaitable> next);
    }
}
