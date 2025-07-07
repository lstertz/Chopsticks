using Chopsticks.Messages.Abstractions;
using System.Threading;
using System.Threading.Tasks;

namespace Chopsticks.Messages.TaskBased
{
    public interface ITaskMessageHandler<TMessage> :
        IMessageHandler<TMessage, Task<HandlingResult>>
    {
        Task<HandlingResult> IMessageHandler<TMessage, Task<HandlingResult>>.Handle(
            TMessage message, CancellationToken token) => HandleAsync(message, token);

        Task<HandlingResult> HandleAsync(TMessage message, CancellationToken token = default);
    }
}
