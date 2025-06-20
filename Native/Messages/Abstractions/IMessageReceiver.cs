using System.Threading;

namespace Chopsticks.Messages.Abstractions
{
    public interface IMessageReceiver<TMessage, TAsync>
    {
        TAsync Receive(TMessage message, CancellationToken token = default);
    }



    // Possibly add extensions to wrap the Receive method, to accommodate different 
    // functionality between a collective and a single receiver.
}
