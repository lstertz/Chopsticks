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
            var source = TryHandlePromiseSource.Pool.Rent();
            source.Init(EvaluateHandle(message));

            return new HandlingResultPromise(source);
        }

        HandlingResultAwaitable IMessageHandler<TMessage>.TryHandleAsync(
            TMessage message, CancellationToken token)
        {
            var source = TryHandlePromiseSource.Pool.Rent();
            source.Init(EvaluateHandle(message));

            return new HandlingResultAwaitable(source);
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
