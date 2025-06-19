using Chopsticks.Messages.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Chopsticks.Messages.TaskBased
{
    public interface ITaskMessageReceiver<TMessage> :
        ITaskMessageAsyncReceiver<TMessage>//,
        //IRegisteredMessageReceiver<TMessage, Task<MessageResult>, CollectiveTaskMessageReceiver<TMessage>>
    {
        private static Task<MessageResult> SuccessfulTaskResult = Task.FromResult(new MessageResult());

        Task<MessageResult> ITaskMessageAsyncReceiver<TMessage>.ReceiveAsync(TMessage message,
            CancellationToken token)
        {
            try
            {
                var result = Receive(message);
                // TODO :: If the result is a success, return the cached successful task result.
            }
            catch (Exception e)
            {
                // TODO :: Build a failed result and wrap a task around around it.
            }

            return SuccessfulTaskResult;
        }


        MessageResult Receive(TMessage t);
    }
}
