using Chopsticks.Messages.Registration;

namespace Chopsticks.Messages.Handlers.Multicast;

public interface IMulticastContextHandler<TMessage, TContext> :
    IContextHandler<TMessage, TContext>,
    IContextHandlerRegistrar<TMessage, TContext>
    where TContext : IMessageContext<TMessage>, new()
{
}
