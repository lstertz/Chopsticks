using Chopsticks.Messages.Abstractions;
using System.Threading;
using System.Threading.Tasks;

namespace Chopsticks.Messages.TaskBased
{
    public interface ITaskMessageHandler<TMessage> :
        IMessageHandler<TMessage>
    {
        HandlingPromise IMessageHandler<TMessage>.Handle(TMessage message)
        {
            var source = new TaskHandlingPromiseSource();  // TODO :: Rent from a pool.
            source.Init(HandleAsync(message).GetAwaiter());

            return new HandlingPromise(source);
        }

        HandlingAwaitable IMessageHandler<TMessage>.HandleAsync(
            TMessage message, CancellationToken token)
        {
            var source = new TaskHandlingPromiseSource();  // TODO :: Rent from a pool.
            source.Init(HandleAsync(message, token).GetAwaiter());

            return new HandlingAwaitable(source);
        }

        new Task HandleAsync(TMessage message, CancellationToken token = default);
    }
}
