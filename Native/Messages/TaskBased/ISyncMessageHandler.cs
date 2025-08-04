using Chopsticks.Messages.Abstractions;
using System;
using System.Threading;

namespace Chopsticks.Messages
{
    public interface ISyncMessageHandler<TMessage> :
        IMessageHandler<TMessage>
    {
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
                Handle(message);
                return HandlingResult.Success;
            }
            catch (Exception ex)
            {
                return HandlingResult.FromException(ex);
            }
        }

        new void Handle(TMessage message);
    }
}
