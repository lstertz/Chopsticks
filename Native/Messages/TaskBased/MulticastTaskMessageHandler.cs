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
        public virtual HandlingPromise Handle(TMessage e)
        {
            if (RegisteredHandlers.Count == 0)
                return HandlingPromise.NoHandlers;

            foreach (var registration in RegisteredHandlers)
                _ = registration.Handler.Handle(e);

            return HandlingPromise.Success; // TODO :: Return a more meaningful result that accounts for async handling and exceptions.
        }

        public virtual async Task<HandlingResult> HandleAsync(TMessage e, 
            CancellationToken token = default)
        {
            if (RegisteredHandlers.Count == 0)
                return HandlingResult.NoHandlers;

            foreach (var registration in RegisteredHandlers)
                await registration.Handler.Handle(e, token);

            return HandlingResult.Success; // TODO :: Return a more meaningful result that accounts for async handling and exceptions.
        }
    }
}
