using Chopsticks.Messages.Abstractions;
using System.Threading;
using System.Threading.Tasks;

namespace Chopsticks.Messages.TaskBased
{
    public class CollectiveTaskMessageReceiver<TMessage> :
        BaseCollectiveMessageReceiver<IBaseMessageReceiver<TMessage>,
            IBaseAsyncMessageReceiver<TMessage, Task>, TMessage, Task>
    {
        // TODO :: Support registering interceptors for the collective.
        //           Support intercepting before entire run and before each receiver.
        protected IIntercept<TMessage>[] Intercepters { get; private set; } = [];


        // TODO :: Note in docs that async handlers are internally managed and awaited.
        public virtual void Receive(TMessage e)  // TODO :: Possibly return result object.
        {
            // TODO :: Progress through both collections based on their registration settings (order).
            foreach (var receiver in Receivers)
                receiver.Receive(e);
            foreach (var receiver in AsyncReceivers)
                _ = receiver.ReceiveAsync(e);  // TODO :: Manage these.
        }

        public virtual async Task ReceiveAsync(TMessage e, 
            CancellationToken token = default, bool runParallel = false)
        {
            // TODO :: Progress through both collections based on their registration settings (order).
            // TODO :: Account for the setting of parallel handling and cancellation.
            foreach (var receiver in Receivers)
                receiver.Receive(e);
            foreach (var receiver in AsyncReceivers)
                await receiver.ReceiveAsync(e);
        }
    }
}
