using Chopsticks.Messages.Handlers;
using System.Threading;

namespace Chopsticks.Messages.Registration;

public readonly struct DefaultMessageContext<TMessage> : IMessageContext<TMessage>
{
    public TMessage Message { get; init; }

    public CancellationToken CancellationToken { get; init; }
}
