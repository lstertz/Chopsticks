using Chopsticks.Messages.Registration;

namespace Chopsticks.Messages.Handlers.Multicast;

public class MulticastMessageHandler<TMessage> :
    MulticastContextHandler<TMessage, DefaultMessageContext<TMessage>>
{ }