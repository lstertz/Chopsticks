using Chopsticks.Messages.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Chopsticks.Messages.TaskBased
{
    public interface ITaskMessageReceiver<TMessage> :
        ITaskMessageAsyncReceiver<TMessage>
    {
        private static Task<MessageResult> Success = Task.FromResult(MessageResult.Success);

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

            return Success;
        }

        MessageResult Receive(TMessage t);
    }
}
