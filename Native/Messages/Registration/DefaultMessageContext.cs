using Chopsticks.Messages.Handlers;
using System.Threading;

namespace Chopsticks.Messages.Registration;

public struct DefaultMessageContext<TMessage> : IMessageContext<TMessage>
{
    public TMessage Message { get; set; }

    public CancellationToken CancellationToken { get; set; }
}
