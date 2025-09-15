using Chopsticks.Messages.Handlers;
namespace Chopsticks.Messages.Registration;

public interface IContextHandlerRegistrar<TMessage, TContext> :
    IMessageHandlerRegistrar<TMessage>
    where TContext : IMessageContext<TMessage>, new()
{
    bool Register(IContextHandler<TMessage, TContext> handler, 
        HandlerRegistrationSettings settings);


    bool Register(IContextHandler<TMessage, TContext> handler) =>
            Register(handler, default);

    void Unregister(IContextHandler<TMessage, TContext> handler);
}
