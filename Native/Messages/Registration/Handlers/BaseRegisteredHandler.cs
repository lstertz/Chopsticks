using Chopsticks.Messages.Handlers;

namespace Chopsticks.Messages.Registration.Handlers;

public abstract class BaseRegisteredHandler<TMessage, TContext> : 
    IRegisteredHandler<TMessage, TContext>
    where TContext : IMessageContext<TMessage>, new()
{
    public int Order { get; init; } = 0;

    public abstract HandlingAwaitable HandleAsync(TContext context);
}
