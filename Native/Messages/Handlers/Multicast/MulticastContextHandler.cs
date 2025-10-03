using Chopsticks.Messages.Interceptors;
using Chopsticks.Messages.Registration;
using Chopsticks.Messages.Registration.Handlers;
using Chopsticks.Messages.Registration.Interceptors;
using System.Linq;
using System.Threading;

namespace Chopsticks.Messages.Handlers.Multicast;

public class MulticastContextHandler<TMessage, TContext> :
    BaseMulticastHandler<TMessage, TContext>,
    IMulticastContextHandler<TMessage, TContext>,
    IMulticastMessageHandler<TMessage>,
    IMessageHandlerRegistrar<TMessage, TContext>
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
                CancellationToken = token
            });


    bool IContextHandlerRegistrar<TMessage, TContext>.Register(
        IContextHandler<TMessage, TContext> handler,
        HandlerRegistrationSettings settings,
        params (IInterceptor<TMessage, TContext>, InterceptorRegistrationSettings)[] interceptors)
    {
        var registeredInterceptors = interceptors.Select((i) => 
            new RegisteredInterceptor<TMessage, TContext>(i.Item1)
            {
                Order = i.Item2.Order
            });

        return AddRegistration(
            new RegisteredContextHandler<TMessage, TContext>(handler)
            {
                Interceptors = [.. registeredInterceptors],
                Order = settings.Order
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
        var registeredInterceptors = interceptors.Select((i) =>
            new RegisteredInterceptor<TMessage, TContext>(i.Item1)
            {
                Order = i.Item2.Order
            });

        return AddRegistration(
            new RegisteredMessageHandler<TMessage, TContext>(handler)
            {
                Interceptors = [.. registeredInterceptors],
                Order = settings.Order
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
