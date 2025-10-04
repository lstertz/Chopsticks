using Chopsticks.Messages.Handlers;
using Chopsticks.Messages.Registration.Handlers;

namespace Chopsticks.Messages.Registration;

public interface IMessageHandlerRegistrar<TMessage>
{
    // TODO :: Accommodate clearing all.

    IRegisteredHandler<TMessage> Register(IMessageHandler<TMessage> handler,
        HandlerRegistrationSettings settings = default);

    void Unregister(IRegisteredHandler registration);
}

public interface IMessageHandlerRegistrar<TMessage, TContext> 
    : IMessageHandlerRegistrar<TMessage>
    where TContext : IMessageContext<TMessage>, new()
{
    // TODO :: Accommodate clearing all.

    IRegisteredHandler<TMessage, TContext> Register(IMessageHandler<TMessage> handler, 
        HandlerRegistrationSettings settings = default);
}
