using Chopsticks.Messages.Registration;

namespace Chopsticks.Messages.Handlers.Multicast;

public interface IMulticastMessageHandler<TMessage> : 
    IMessageHandler<TMessage>,
    IMessageHandlerRegistrar<TMessage>
{
}
