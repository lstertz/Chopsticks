using System;
using System.Threading;
using Chopsticks.Messages.Handlers.Sources;

namespace Chopsticks.Messages.Handlers
{
    public interface ISyncMessageHandler<TMessage> :
        IMessageHandler<TMessage>
    {
        void Handle(TMessage message);


        HandlingResultPromise IMessageHandler<TMessage>.TryHandle(TMessage message)
        {
            return new HandlingResultPromise(EvaluateHandle(message));
        }

        HandlingResultAwaitable IMessageHandler<TMessage>.TryHandleAsync(
            TMessage message, CancellationToken token)
        {
            return new HandlingResultAwaitable(EvaluateHandle(message));
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
