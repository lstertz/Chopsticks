using Chopsticks.Messages.Handlers;
using Chopsticks.Messages.Interceptors;
using Chopsticks.Messages.Registration.Handlers;
namespace Chopsticks.Messages.Registration;

public interface IContextHandlerRegistrar<TMessage, TContext> :
    IMessageHandlerRegistrar<TMessage, TContext>
    where TContext : IMessageContext<TMessage>, new()
{
    IRegisteredHandler Register(IContextHandler<TMessage, TContext> handler, 
        HandlerRegistrationSettings settings,
        params (IContextInterceptor<TMessage, TContext>, InterceptorRegistrationSettings)[] interceptors);


    IRegisteredHandler Register(IContextHandler<TMessage, TContext> handler,
        params (IContextInterceptor<TMessage, TContext>, InterceptorRegistrationSettings)[] interceptors) =>
            Register(handler, default, interceptors);

    void Unregister(IRegisteredHandler registration);
}
