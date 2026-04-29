using Chopsticks.Messages.Handlers;
using Chopsticks.Messages.Registration.Handlers;
namespace Chopsticks.Messages.Registration;

public interface IContextHandlerRegistrar<TMessage, TContext> :
    IMessageHandlerRegistrar<TMessage, TContext>
    where TContext : IMessageContext<TMessage>, new()
{
    IRegisteredHandler<TMessage, TContext> Register(IContextHandler<TMessage, TContext> handler, 
        HandlerRegistrationSettings settings);

    IRegisteredHandler<TMessage, TContext> Register(IContextHandler<TMessage, TContext> handler) =>
            Register(handler, default);
}
