using Chopsticks.Messages.Abstractions;
using System.Threading;
using System.Threading.Tasks;

namespace Chopsticks.Messages.TaskBased
{
    public interface IAsyncTaskMessageHandler<TMessage> :
        ITaskMessageHandler<TMessage>
    {
        HandlingPromise IMessageHandler<TMessage, Task<HandlingResult>>.Handle(TMessage message)
        {
            // TODO :: Wrap in the promise - HandleAsync(message);
            return default;
        }

        async Task<HandlingResult> IMessageHandler<TMessage, Task<HandlingResult>>.HandleAsync(
            TMessage message, CancellationToken token)
        {
            // TODO :: Handle conditions to produce the result.
            await HandleAsync(message);
            return HandlingResult.Success;
        }

        new Task HandleAsync(TMessage message, CancellationToken token = default);
    }
}
