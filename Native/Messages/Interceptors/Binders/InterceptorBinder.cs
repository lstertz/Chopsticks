using Chopsticks.Messages.Handlers;
using System;

namespace Chopsticks.Messages.Interceptors.Binders
{
    public class InterceptorBinder<TMessage, TContext> :
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
        public InterceptorBinder(IInterceptor interceptor)
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
