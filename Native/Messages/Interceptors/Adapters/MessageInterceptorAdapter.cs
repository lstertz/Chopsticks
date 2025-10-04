using Chopsticks.Messages.Handlers;
using System;

namespace Chopsticks.Messages.Interceptors.Adapters
{
    public class MessageInterceptorAdapter<TMessage, TContext> :
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


        private readonly IMessageInterceptor<TMessage> _interceptor;
        public MessageInterceptorAdapter(IMessageInterceptor<TMessage> interceptor)
        {
            _interceptor = interceptor;
        }

        public HandlingAwaitable InterceptAsync(TContext context, 
            Func<TContext, HandlingAwaitable> next)
        {
            var nextInvoker = new NextInvoker(next, context);
            return _interceptor.InterceptAsync(context.Message,
                context.CancellationToken, nextInvoker.InvokeNext);
        }
    }
}
