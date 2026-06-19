using Chopsticks.Messages.Handlers.Sources;
using System;
using System.Threading;

namespace Chopsticks.Messages.Handlers
{
    public interface ISyncContextHandler<TMessage, TContext> :
        IContextHandler<TMessage, TContext>
        where TContext : IMessageContext<TMessage>, new()
    {
        void Handle(TContext context);



        /// <inheritdoc/>
        HandlingCompletionPromise IContextHandler<TMessage, TContext>.Handle(TContext context,
            SynchronizationContext? asyncContext) =>
            new(EvaluateHandle(context), asyncContext);

        HandlingResultPromise IContextHandler<TMessage, TContext>.TryHandle(TContext context) => 
            new(EvaluateHandle(context));

        HandlingResultAwaitable IContextHandler<TMessage, TContext>.TryHandleAsync(TContext context)
        {
            var source = TryHandlePromiseSource.Pool.Rent();
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
