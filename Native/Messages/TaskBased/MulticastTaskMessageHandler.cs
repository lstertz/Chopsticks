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

            foreach (var registration in RegisteredHandlers)
                _ = registration.Handler.Handle(message);

            return HandlingPromise.Success; // TODO :: Return a more meaningful result that accounts for async handling and exceptions.
        }

        public virtual async Task<HandlingResult> HandleAsync(TMessage message,
            CancellationToken token = default)
        {
            var result = HandlingResult.NoHandlers;

            foreach (var registration in RegisteredHandlers)
                result.MergeWith(await registration.Handler.Handle(message, token));

            return result;
        }
    }
}
