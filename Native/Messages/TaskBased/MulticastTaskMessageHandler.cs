using Chopsticks.Messages.Abstractions;
using System.Threading;
using System.Threading.Tasks;

namespace Chopsticks.Messages.TaskBased
{
    public class MulticastTaskMessageHandler<TMessage> :
        BaseMulticastMessageHandler<TMessage, Task<MessageResult>>,
        IMulticastTaskMessageHandler<TMessage>,
        ITaskMessageHandlerRegistrar<TMessage>
    {
        public virtual MessageResult Handle(TMessage e)
        {
            if (RegisteredHandlers.Count == 0)
                return MessageResult.NoHandlers;

            foreach (var registration in RegisteredHandlers)
                _ = registration.Handler.Handle(e);

            return MessageResult.Success; // TODO :: Return a more meaningful result that accounts for async handling and exceptions.
        }

        public virtual async Task<MessageResult> HandleAsync(TMessage e, 
            CancellationToken token = default)
        {
            if (RegisteredHandlers.Count == 0)
                return MessageResult.NoHandlers;

            foreach (var registration in RegisteredHandlers)
                await registration.Handler.Handle(e, token);

            return MessageResult.Success; // TODO :: Return a more meaningful result that accounts for async handling and exceptions.
        }
    }
}
