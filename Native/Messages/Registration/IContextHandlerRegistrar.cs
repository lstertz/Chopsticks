using Chopsticks.Messages.Handlers;
using Chopsticks.Messages.Interception;
namespace Chopsticks.Messages.Registration;

public interface IContextHandlerRegistrar<TMessage, TContext> :
    IHandlerRegistrar<IContextHandler<TMessage, TContext>, IIntercept<TMessage>>
    where TContext : IMessageContext<TMessage>
{
}
