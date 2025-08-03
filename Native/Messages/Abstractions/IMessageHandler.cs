using System.Threading;

namespace Chopsticks.Messages.Abstractions
{
    public interface IMessageHandler<TMessage>
    {
        HandlingPromise Handle(TMessage message);

        HandlingAwaitable HandleAsync(TMessage message, CancellationToken token = default);
    }



    // Possibly add extensions to wrap the Handle method, to accommodate different 
    // functionality between a multicast and a single handler.
}
