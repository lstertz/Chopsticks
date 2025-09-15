using Chopsticks.Messages.Handlers;
using Chopsticks.Messages.Interceptors;

namespace Chopsticks.Messages.Registration;

public interface IContextHandlerRegistrar<TMessage, TContext> :
    IMessageHandlerRegistrar<TMessage>
    where TContext : IMessageContext<TMessage>, new()
{
    bool Register(IContextHandler<TMessage, TContext> handler, 
        HandlerRegistrationSettings settings, 
        params IInterceptor<TMessage, TContext>[] interceptors);


    bool Register(IContextHandler<TMessage, TContext> handler, 
        params IInterceptor<TMessage, TContext>[] interceptors) =>
            Register(handler, default, interceptors);

    void Unregister(IContextHandler<TMessage, TContext> handler);
}
