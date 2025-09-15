using Chopsticks.Messages.Handlers.Sources;
using System;

namespace Chopsticks.Messages.Handlers
{
    public interface ISyncContextHandler<TMessage, TContext> :
        IContextHandler<TMessage, TContext>
        where TContext : IMessageContext<TMessage>, new()
    {
        new void Handle(TContext context);


        HandlingPromise IContextHandler<TMessage, TContext>.Handle(TContext context)
        {
            var source = new SyncHandlingPromiseSource();  // TODO :: Rent from a pool.
            source.Init(EvaluateHandle(context));

            return new HandlingPromise(source);
        }

        HandlingAwaitable IContextHandler<TMessage, TContext>.HandleAsync(TContext context)
        {
            var source = new SyncHandlingPromiseSource();  // TODO :: Rent from a pool.
            source.Init(EvaluateHandle(context));

            return new HandlingAwaitable(source);
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
