using Chopsticks.Messages.Handlers.Sources;
using System.Threading;
using System.Threading.Tasks;

namespace Chopsticks.Messages.Handlers
{
    public interface ITaskMessageHandler<TMessage> :
        IMessageHandler<TMessage>
    {
        new Task HandleAsync(TMessage message, CancellationToken token = default);


        HandlingResultPromise IMessageHandler<TMessage>.TryHandle(TMessage message)
        {
            var source = TryHandleAsyncTaskPromiseSource.Pool.Rent();
            source.Init(HandleAsync(message).GetAwaiter());

            return new HandlingResultPromise(source);
        }

        HandlingResultAwaitable IMessageHandler<TMessage>.TryHandleAsync(
            TMessage message, CancellationToken token)
        {
            // Maintain explicit cast to ensure dispatching to the correct method.
            Task task = (this as ITaskMessageHandler<TMessage>).HandleAsync(message, token);

            var source = TryHandleAsyncTaskPromiseSource.Pool.Rent();
            source.Init(task.GetAwaiter());

            return new HandlingResultAwaitable(source);
        }

    }
}
