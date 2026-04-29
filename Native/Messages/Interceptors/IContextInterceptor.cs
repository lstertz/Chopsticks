using Chopsticks.Messages.Handlers;
using System;

namespace Chopsticks.Messages.Interceptors
{
    public interface IContextInterceptor<TMessage, TContext>
        where TContext : IMessageContext<TMessage>
    {
        HandlingResultAwaitable InterceptAsync(TContext context, 
            Func<TContext, HandlingResultAwaitable> next);
    }
}
