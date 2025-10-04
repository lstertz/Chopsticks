using Chopsticks.Messages.Handlers;
using Chopsticks.Messages.Interceptors;
namespace Chopsticks.Messages.Registration;

public interface IMessageHandlerRegistrar<TMessage>
{
    // TODO :: Accommodate clearing all.

    bool Register(IMessageHandler<TMessage> handler, HandlerRegistrationSettings settings);


    bool Register(IMessageHandler<TMessage> handler) =>
        Register(handler, default);

    void Unregister(IMessageHandler<TMessage> handler);
}

public interface IMessageHandlerRegistrar<TMessage, TContext> 
    : IMessageHandlerRegistrar<TMessage>
    where TContext : IMessageContext<TMessage>, new()
{
    // TODO :: Accommodate clearing all.

    bool Register(IMessageHandler<TMessage> handler, 
        HandlerRegistrationSettings settings,
        params (IContextInterceptor<TMessage, TContext>, InterceptorRegistrationSettings)[] interceptors);


    bool Register(IMessageHandler<TMessage> handler,
        params (IContextInterceptor<TMessage, TContext>, InterceptorRegistrationSettings)[] interceptors) =>
        Register(handler, default, interceptors);
}
