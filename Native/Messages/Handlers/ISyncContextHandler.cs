using Chopsticks.Messages.Handlers.Sources;
using System;

namespace Chopsticks.Messages.Handlers
{
    public interface ISyncContextHandler<TMessage, TContext> :
        IContextHandler<TMessage, TContext>
        where TContext : IMessageContext<TMessage>, new()
    {
        new void Handle(TContext context);


        HandlingResultPromise IContextHandler<TMessage, TContext>.TryHandle(TContext context)
        {
            var source = new TryHandlePromiseSource();  // TODO :: Rent from a pool.
            source.Init(EvaluateHandle(context));

            return new HandlingResultPromise(source);
        }

        HandlingResultAwaitable IContextHandler<TMessage, TContext>.TryHandleAsync(TContext context)
        {
            var source = new TryHandlePromiseSource();  // TODO :: Rent from a pool.
            source.Init(EvaluateHandle(context));

            return new HandlingResultAwaitable(source);
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
