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


    IRegisteredHandler<TMessage, TContext> IContextHandlerRegistrar<TMessage, TContext>.Register(
        IContextHandler<TMessage, TContext> handler,
        HandlerRegistrationSettings settings)
    {
        var registration = new RegisteredContextHandler<TMessage, TContext>(handler)
        {
            Order = settings.Order
        };
        AddRegistration(registration);

        return registration;
    }

    IRegisteredHandler<TMessage> IMessageHandlerRegistrar<TMessage>.Register(
        IMessageHandler<TMessage> handler,
        HandlerRegistrationSettings settings)
    {
        var registration = new RegisteredMessageHandler<TMessage, TContext>(handler)
        {
            Order = settings.Order
        };
        AddRegistration(registration);
        
        return registration;
    }

    IRegisteredHandler<TMessage, TContext> IMessageHandlerRegistrar<TMessage, TContext>.Register(
        IMessageHandler<TMessage> handler,
        HandlerRegistrationSettings settings)
    {
        var registration = new RegisteredMessageHandler<TMessage, TContext>(handler)
        {
            Order = settings.Order
        };
        AddRegistration(registration);

        return registration;
    }

    void IContextHandlerRegistrar<TMessage, TContext>.Unregister(
        IRegisteredHandler registration) => RemoveRegistration(registration);

    void IMessageHandlerRegistrar<TMessage>.Unregister(
        IRegisteredHandler registration) => RemoveRegistration(registration);
}
