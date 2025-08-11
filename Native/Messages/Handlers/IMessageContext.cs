using System.Threading;

namespace Chopsticks.Messages.Handlers
{
    public interface IMessageContext<TMessage>
    {
        CancellationToken CancellationToken { get; }

        TMessage Message { get; init; }
    }
}
