using Chopsticks.Messages.Handlers;
using Chopsticks.Messages.Registration.Handlers;

namespace Chopsticks.Messages.Registration;

public abstract class MessageHandlerRegistrar<TMessage> :
    BaseMessageHandlerRegistrar<TMessage, DefaultMessageContext<TMessage>>,
    IMessageHandlerRegistrar<TMessage>
{
    bool IMessageHandlerRegistrar<TMessage>.Register(
        IMessageHandler<TMessage> handler,
        RegistrationSettings settings)
    {
        return AddRegistration(
            new RegisteredMessageHandler<TMessage, DefaultMessageContext<TMessage>>(handler)
            {
                Order = settings.Order
            });
    }

    void IMessageHandlerRegistrar<TMessage>.Unregister(
        IMessageHandler<TMessage> handler)
    {
        // TODO :: Optimize with an internal registration ID.
        RemoveRegistration(
            new RegisteredMessageHandler<TMessage, DefaultMessageContext<TMessage>>(handler));
    }
}
