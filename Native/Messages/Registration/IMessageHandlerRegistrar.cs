using Chopsticks.Messages.Handlers;
using Chopsticks.Messages.Interception;
namespace Chopsticks.Messages.Registration;

public interface IMessageHandlerRegistrar<TMessage> :
    IHandlerRegistrar<IMessageHandler<TMessage>, IIntercept<TMessage>>
{
}
