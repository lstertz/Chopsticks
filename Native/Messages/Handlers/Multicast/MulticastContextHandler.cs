using Chopsticks.Messages.Interceptors;
using Chopsticks.Messages.Registration;
using Chopsticks.Messages.Registration.Handlers;
using System.Threading;

namespace Chopsticks.Messages.Handlers.Multicast;

public class MulticastContextHandler<TMessage, TContext> :
    BaseMulticastHandler<TMessage, TContext>,
    IMulticastContextHandler<TMessage, TContext>,
    IMulticastMessageHandler<TMessage>
    where TContext : IMessageContext<TMessage>, new()
{
    public HandlingPromise Handle(TContext context) =>
        _dispatchPipeline(context).ToPromise();

    public HandlingPromise Handle(TMessage message) =>
        _dispatchPipeline(new TContext()
        {
            Message = message,
        }).ToPromise();

    public HandlingAwaitable HandleAsync(TContext context) =>
        _dispatchPipeline(context);

    public HandlingAwaitable HandleAsync(TMessage message,
        CancellationToken token = default) =>
            _dispatchPipeline(new TContext()
            {
                Message = message,
            });


    bool IContextHandlerRegistrar<TMessage, TContext>.Register(
        IContextHandler<TMessage, TContext> handler,
        HandlerRegistrationSettings settings,
        params (IInterceptor<TMessage, TContext>, InterceptorRegistrationSettings)[] interceptors)
    {
        return AddRegistration(
            new RegisteredContextHandler<TMessage, TContext>(handler)
            {
                Order = settings.Order
                // TODO :: Set registered interceptors.
            });
    }

    bool IMessageHandlerRegistrar<TMessage>.Register(
        IMessageHandler<TMessage> handler,
        HandlerRegistrationSettings settings)
    {
        return AddRegistration(
            new RegisteredMessageHandler<TMessage, TContext>(handler)
            {
                Order = settings.Order
            });
    }

    bool IMessageHandlerRegistrar<TMessage, TContext>.Register(
        IMessageHandler<TMessage> handler,
        HandlerRegistrationSettings settings,
        params (IInterceptor<TMessage, TContext>, InterceptorRegistrationSettings)[] interceptors)
    {
        return AddRegistration(
            new RegisteredMessageHandler<TMessage, TContext>(handler)
            {
                Order = settings.Order
                // TODO :: Set registered interceptors.
            });
    }

    void IContextHandlerRegistrar<TMessage, TContext>.Unregister(
        IContextHandler<TMessage, TContext> handler)
    {
        // TODO :: Optimize with an internal registration ID.
        RemoveRegistration(
            new RegisteredContextHandler<TMessage, TContext>(handler));
    }

    void IMessageHandlerRegistrar<TMessage>.Unregister(
        IMessageHandler<TMessage> handler)
    {
        // TODO :: Optimize with an internal registration ID.
        RemoveRegistration(
            new RegisteredMessageHandler<TMessage, TContext>(handler));
    }
}
