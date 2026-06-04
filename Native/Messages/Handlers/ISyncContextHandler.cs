using System;

namespace Chopsticks.Messages.Handlers
{
    public interface ISyncContextHandler<TMessage, TContext> :
        IContextHandler<TMessage, TContext>
        where TContext : IMessageContext<TMessage>, new()
    {
        void Handle(TContext context);


        HandlingResultPromise IContextHandler<TMessage, TContext>.TryHandle(TContext context)
        {
            return new HandlingResultPromise(EvaluateHandle(context));
        }

        HandlingResultAwaitable IContextHandler<TMessage, TContext>.TryHandleAsync(TContext context)
        {
            return new HandlingResultAwaitable(EvaluateHandle(context));
        }

        private HandlingResult EvaluateHandle(TContext context)
        {
            try
            {
                // Maintain explicit cast to ensure dispatching to the correct method.
                (this as ISyncContextHandler<TMessage, TContext>).Handle(context);
                return HandlingResult.Success;
            }
            catch (Exception ex)
            {
                return HandlingResult.FromException(ex);
            }
        }
    }
}
