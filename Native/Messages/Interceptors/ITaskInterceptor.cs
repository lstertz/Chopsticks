using Chopsticks.Messages.Handlers;
using Chopsticks.Messages.Handlers.Sources;
using System;
using System.Threading.Tasks;

namespace Chopsticks.Messages.Interceptors
{
    public interface ITaskInterceptor<TMessage, TContext> : IInterceptor<TMessage, TContext>
        where TContext : IMessageContext<TMessage>
    {
        HandlingAwaitable IInterceptor<TMessage, TContext>.InterceptAsync(
            TContext context, Func<TContext, HandlingAwaitable> next)
        {
            var source = new TaskHandlingPromiseSource();  // TODO :: Rent from a pool.
            source.Init(InterceptAsync(context, next).GetAwaiter());

            return new HandlingAwaitable(source);
        }

        new Task InterceptAsync(TContext context, Func<TContext, HandlingAwaitable> next);
    }
}
