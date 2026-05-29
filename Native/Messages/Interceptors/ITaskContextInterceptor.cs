using Chopsticks.Messages.Handlers;
using Chopsticks.Messages.Handlers.Sources;
using System;
using System.Threading.Tasks;

namespace Chopsticks.Messages.Interceptors
{
    public interface ITaskContextInterceptor<TMessage, TContext> : 
        IContextInterceptor<TMessage, TContext>
        where TContext : IMessageContext<TMessage>
    {
        new Task InterceptAsync(TContext context, Func<TContext, HandlingResultAwaitable> next);

        HandlingResultAwaitable IContextInterceptor<TMessage, TContext>.InterceptAsync(
            TContext context, Func<TContext, HandlingResultAwaitable> next)
        {
            var source = TryHandleAsyncPromiseSource.Rent();
            source.Init((this as ITaskContextInterceptor<TMessage, TContext>).InterceptAsync(
                context, next).GetAwaiter());

            return new HandlingResultAwaitable(source);
        }
    }
}
