using Chopsticks.Messages.Handlers;
using Chopsticks.Messages.Registration.Handlers;

namespace Chopsticks.Messages.Registration;

public abstract class ContextHandlerRegistrar<TMessage, TContext> :
    BaseMessageHandlerRegistrar<TMessage, TContext>,
    IContextHandlerRegistrar<TMessage, TContext>
    where TContext : IMessageContext<TMessage>, new()
{
    public bool Register(IContextHandler<TMessage, TContext> handler, 
        HandlerRegistrationSettings settings)
    {
        // TODO :: Add interceptors to the handler if needed.
        return AddRegistration(new RegisteredContextHandler<TMessage, TContext>(handler)
        {
            Order = settings.Order
        });
    }

    public bool Register(IMessageHandler<TMessage> handler, HandlerRegistrationSettings settings)
    {
        // TODO :: Add interceptors to the handler if needed.
        return AddRegistration(new RegisteredMessageHandler<TMessage, TContext>(handler)
        {
            Order = settings.Order
        });
    }

    public void Unregister(IContextHandler<TMessage, TContext> handler)
    {
        // TODO :: Optimize with an internal registration ID.
        RemoveRegistration(new RegisteredContextHandler<TMessage, TContext>(handler));
    }

    public void Unregister(IMessageHandler<TMessage> handler)
    {
        RemoveRegistration(new RegisteredMessageHandler<TMessage, TContext>(handler));
    }
}
