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
        public virtual MessageResult Receive(TMessage e)  // TODO :: Possibly return result object.
        {
            // TODO :: Progress through both collections based on their registration settings (order).
           // foreach (var receiver in Receivers)
             //   receiver.Receive(e);
            foreach (var receiver in Receivers)
                _ = receiver.Receive(e);  // TODO :: Manage these.

            return new MessageResult(); // TODO :: Return a meaningful result.
        }

        public virtual async Task<MessageResult> ReceiveAsync(TMessage e, 
            CancellationToken token = default)
        {
            // TODO :: Progress through both collections based on their registration settings (order).
            // TODO :: Account for the setting of parallel handling and cancellation.
            //foreach (var receiver in Receivers)
              //  receiver.Receive(e);
            foreach (var receiver in Receivers)
                await receiver.Receive(e, token);

            return new MessageResult(); // TODO :: Return a meaningful result.
        }
    }
}
