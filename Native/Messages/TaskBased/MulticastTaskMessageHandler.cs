using Chopsticks.Messages.Abstractions;
using System.Threading;
using System.Threading.Tasks;

namespace Chopsticks.Messages.TaskBased
{
    public class MulticastTaskMessageHandler<TMessage> :
        BaseMulticastMessageHandler<TMessage, Task<HandlingResult>>,
        IMulticastTaskMessageHandler<TMessage>,
        ITaskMessageHandlerRegistrar<TMessage>
    {
        public virtual HandlingPromise Handle(TMessage message)
        {
            if (RegisteredHandlers.Count == 0)
                return HandlingPromise.NoHandlers;

            // TODO :: Build a promise that wraps the HandleAsync call and return that.

            foreach (var registration in RegisteredHandlers)
                _ = registration.Handler.Handle(message);

            return HandlingPromise.Success;
        }

        public virtual async Task<HandlingResult> HandleAsync(TMessage message,
            CancellationToken token = default)
        {
            var result = HandlingResult.NoHandlers;

            foreach (var registration in RegisteredHandlers)
                result.MergeWith(await registration.Handler.HandleAsync(message, token));

            return result;
        }
    }
}
