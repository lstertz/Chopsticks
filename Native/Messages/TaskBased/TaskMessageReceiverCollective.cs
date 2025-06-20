using Chopsticks.Messages.Abstractions;
using System.Threading;
using System.Threading.Tasks;

namespace Chopsticks.Messages.TaskBased
{
    public class TaskMessageReceiverCollective<TMessage> :
        BaseMessageReceiverCollective<TMessage, Task<MessageResult>>,
        ITaskMessageReceiver<TMessage>,
        ITaskMessageReceiverCollective<TMessage>
    {
        // TODO :: Note in docs that async handlers are captured with updates propagated through MessageResult.
        public virtual MessageResult Receive(TMessage e)
        {
            if (RegisteredReceivers.Count == 0)
                return MessageResult.NoReceivers;

            foreach (var registration in RegisteredReceivers)
                _ = registration.Receiver.Receive(e);  // TODO :: Manage the async handling.

            return MessageResult.Success; // TODO :: Return a meaningful result that accounts for async handling and exceptions.
        }

        public virtual async Task<MessageResult> ReceiveAsync(TMessage e, 
            CancellationToken token = default)
        {
            if (RegisteredReceivers.Count == 0)
                return MessageResult.NoReceivers;

            foreach (var registration in RegisteredReceivers)
                await registration.Receiver.Receive(e, token);

            return MessageResult.Success; // TODO :: Return a meaningful result that accounts for async handling and exceptions.
        }
    }
}
