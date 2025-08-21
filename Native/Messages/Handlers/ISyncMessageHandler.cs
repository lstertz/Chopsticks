using Chopsticks.Messages.Handlers.Sources;
using System;
using System.Threading;

namespace Chopsticks.Messages.Handlers
{
    public interface ISyncMessageHandler<TMessage> :
        IMessageHandler<TMessage>
    {
        new void Handle(TMessage message);


        HandlingPromise IMessageHandler<TMessage>.Handle(TMessage message)
        {
            var source = new SyncHandlingPromiseSource();  // TODO :: Rent from a pool.
            source.Init(EvaluateHandle(message));

            return new HandlingPromise(source);
        }

        HandlingAwaitable IMessageHandler<TMessage>.HandleAsync(
            TMessage message, CancellationToken token)
        {
            var source = new SyncHandlingPromiseSource();  // TODO :: Rent from a pool.
            source.Init(EvaluateHandle(message));

            return new HandlingAwaitable(source);
        }

        private HandlingResult EvaluateHandle(TMessage message)
        {
            try
            {
                // Maintain explicit cast to ensure dispatching to the correct method.
                (this as ISyncMessageHandler<TMessage>).Handle(message);
                return HandlingResult.Success;
            }
            catch (Exception ex)
            {
                return HandlingResult.FromException(ex);
            }
        }
    }
}
