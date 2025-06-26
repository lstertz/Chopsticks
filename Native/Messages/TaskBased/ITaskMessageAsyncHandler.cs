using Chopsticks.Messages.Abstractions;
using System.Threading;
using System.Threading.Tasks;

namespace Chopsticks.Messages.TaskBased
{
    public interface ITaskMessageAsyncHandler<TMessage> :
        IMessageHandler<TMessage, Task<MessageResult>>
    {
        Task<MessageResult> IMessageHandler<TMessage, Task<MessageResult>>.Handle(
            TMessage message, CancellationToken token) => HandleAsync(message, token);

        Task<MessageResult> HandleAsync(TMessage message, CancellationToken token = default);
    }
}
