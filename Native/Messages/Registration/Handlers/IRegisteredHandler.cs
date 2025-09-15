using Chopsticks.Messages.Handlers;

namespace Chopsticks.Messages.Registration.Handlers;

public interface IRegisteredHandler<TMessage, TContext>
    where TContext : IMessageContext<TMessage>, new()
{
    int Order { get; }

    HandlingAwaitable HandleAsync(TContext context);
}
