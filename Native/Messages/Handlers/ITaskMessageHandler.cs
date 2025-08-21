using Chopsticks.Messages.Handlers.Sources;
using System.Threading;
using System.Threading.Tasks;

namespace Chopsticks.Messages.Handlers
{
    public interface ITaskMessageHandler<TMessage> :
        IMessageHandler<TMessage>
    {
        new Task HandleAsync(TMessage message, CancellationToken token = default);


        HandlingPromise IMessageHandler<TMessage>.Handle(TMessage message)
        {
            var source = new TaskHandlingPromiseSource();  // TODO :: Rent from a pool.
            source.Init(HandleAsync(message).GetAwaiter());

            return new HandlingPromise(source);
        }

        HandlingAwaitable IMessageHandler<TMessage>.HandleAsync(
            TMessage message, CancellationToken token)
        {
            // Maintain explicit cast to ensure dispatching to the correct method.
            Task task = (this as ITaskMessageHandler<TMessage>).HandleAsync(message, token);

            var source = new TaskHandlingPromiseSource();  // TODO :: Rent from a pool.
            source.Init(task.GetAwaiter());

            return new HandlingAwaitable(source);
        }

    }
}
