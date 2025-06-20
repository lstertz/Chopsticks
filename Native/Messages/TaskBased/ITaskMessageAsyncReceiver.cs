using Chopsticks.Messages.Abstractions;
using System.Threading;
using System.Threading.Tasks;

namespace Chopsticks.Messages.TaskBased
{
    public interface ITaskMessageAsyncReceiver<TMessage> :
        IRegisteredMessageReceiver<TMessage, Task<MessageResult>>
    {
        public static CollectiveTaskMessageReceiver<TMessage> DefaultCollective { get; } =
            new CollectiveTaskMessageReceiver<TMessage>();


        Task<MessageResult> IRegisteredMessageReceiver<TMessage, Task<MessageResult>>.Receive(
            TMessage message, CancellationToken token) => ReceiveAsync(message, token);

        Task<MessageResult> ReceiveAsync(TMessage message, CancellationToken token = default);
    }
}
