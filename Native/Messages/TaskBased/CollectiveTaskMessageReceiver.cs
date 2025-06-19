using Chopsticks.Messages.Abstractions;
using System.Threading;
using System.Threading.Tasks;

namespace Chopsticks.Messages.TaskBased
{
    public class CollectiveTaskMessageReceiver<TMessage> :
        BaseCollectiveMessageReceiver<TMessage, Task<MessageResult>>,
        ITaskMessageReceiver<TMessage>,
        ICollectiveTaskMessageReceiver<TMessage>
    {
        // TODO :: Note in docs that async handlers are captured with updates propagated through MessageResult.
        public virtual MessageResult Receive(TMessage e)
        {
            foreach (var registration in RegisteredReceivers)
                _ = registration.Receiver.Receive(e);  // TODO :: Manage the async handling.

            return new MessageResult(); // TODO :: Return a meaningful result.
        }

        public virtual async Task<MessageResult> ReceiveAsync(TMessage e, 
            CancellationToken token = default)
        {
            foreach (var registration in RegisteredReceivers)
                await registration.Receiver.Receive(e, token);

            return new MessageResult(); // TODO :: Return a meaningful result.
        }
    }
}
