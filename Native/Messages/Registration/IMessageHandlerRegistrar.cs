using Chopsticks.Messages.Handlers;
using Chopsticks.Messages.Interceptors;
using Chopsticks.Messages.Registration.Handlers;

namespace Chopsticks.Messages.Registration;

public interface IMessageHandlerRegistrar<TMessage>
{
    // TODO :: Accommodate clearing all.

    IRegisteredHandler Register(IMessageHandler<TMessage> handler, 
        HandlerRegistrationSettings settings);


    IRegisteredHandler Register(IMessageHandler<TMessage> handler) =>
        Register(handler, default);

    void Unregister(IRegisteredHandler registration);
}

public interface IMessageHandlerRegistrar<TMessage, TContext> 
    : IMessageHandlerRegistrar<TMessage>
    where TContext : IMessageContext<TMessage>, new()
{
    // TODO :: Accommodate clearing all.

    IRegisteredHandler Register(IMessageHandler<TMessage> handler, 
        HandlerRegistrationSettings settings,
        params (IContextInterceptor<TMessage, TContext>, InterceptorRegistrationSettings)[] interceptors);


    IRegisteredHandler Register(IMessageHandler<TMessage> handler,
        params (IContextInterceptor<TMessage, TContext>, InterceptorRegistrationSettings)[] interceptors) =>
        Register(handler, default, interceptors);
}
