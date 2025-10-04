using Chopsticks.Messages.Handlers;
using System;

namespace Chopsticks.Messages.Interceptors.Adapters
{
    public class ContractInterceptorAdapter<TMessage, TContext, TContract> :
        IContextInterceptor<TMessage, TContext>
        where TContext : IMessageContext<TMessage>
    {
        private readonly struct NextInvoker(
            Func<TContext, HandlingAwaitable> next, 
            TContext fallbackContext)
        {
            public HandlingAwaitable InvokeNext(TContract contract)
            {
                if (contract is not TContext context)
                    return next(fallbackContext);

                return next(context);
            }
        }

        private readonly IContractInterceptor<TContract> _interceptor;
        public ContractInterceptorAdapter(IContractInterceptor<TContract> interceptor, 
            ContractInterceptorMode mode)
        {
            if (mode == ContractInterceptorMode.Required &&
                !typeof(TContract).IsAssignableFrom(typeof(TContext)))
            {
                throw new InvalidOperationException(
                    $"The context type {typeof(TContext).FullName} " +
                    $"must implement the contract type {typeof(TContract).FullName} " +
                    $"for a required contractual interceptor.");
            }

            _interceptor = interceptor;
        }

        public HandlingAwaitable InterceptAsync(TContext context, 
            Func<TContext, HandlingAwaitable> next)
        {
            if (context is not TContract contractContext)
            {
                return next(context);
            }

            var nextInvoker = new NextInvoker(next, context);
            return _interceptor.InterceptAsync(contractContext, 
                context.CancellationToken, nextInvoker.InvokeNext);
        }
    }
}
