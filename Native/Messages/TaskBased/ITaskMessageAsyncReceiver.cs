using Chopsticks.Messages.Abstractions;
using System.Threading;
using System.Threading.Tasks;

namespace Chopsticks.Messages.TaskBased
{
    public interface ITaskMessageAsyncReceiver<TMessage> :
        IMessageReceiver<TMessage, Task<MessageResult>>
    {
        public static TaskMessageReceiverCollective<TMessage> DefaultCollective { get; } =
            new TaskMessageReceiverCollective<TMessage>();


        Task<MessageResult> IMessageReceiver<TMessage, Task<MessageResult>>.Receive(
            TMessage message, CancellationToken token) => ReceiveAsync(message, token);

        Task<MessageResult> ReceiveAsync(TMessage message, CancellationToken token = default);
    }
}
