using Chopsticks.Messages.Handlers;
using System;

namespace Chopsticks.Messages.Interceptors.Adapters
{
    public class InterceptorAdapter<TMessage, TContext> :
        IContextInterceptor<TMessage, TContext>
        where TContext : IMessageContext<TMessage>
    {
        private readonly struct NextInvoker(
            Func<TContext, HandlingAwaitable> next,
            TContext context)
        {
            public HandlingAwaitable InvokeNext()
            {
                return next(context);
            }
        }


        private readonly IInterceptor _interceptor;
        public InterceptorAdapter(IInterceptor interceptor)
        {
            _interceptor = interceptor;
        }

        public HandlingAwaitable InterceptAsync(TContext context,
            Func<TContext, HandlingAwaitable> next)
        {
            var nextInvoker = new NextInvoker(next, context);
            return _interceptor.InterceptAsync(
                context.CancellationToken, nextInvoker.InvokeNext);
        }
    }
}
