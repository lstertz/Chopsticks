using System.Threading;

namespace Chopsticks.Messages.Handlers
{
    public interface IMessageHandler<TMessage>
    {
        HandlingPromise Handle(TMessage message);

        HandlingAwaitable HandleAsync(TMessage message, CancellationToken token = default);
    }
}
