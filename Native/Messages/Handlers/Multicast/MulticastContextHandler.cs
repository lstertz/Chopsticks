using Chopsticks.Messages.Registration;
using Chopsticks.Messages.Registration.Handlers;
using System.Threading;

namespace Chopsticks.Messages.Handlers.Multicast;

public class MulticastContextHandler<TMessage, TContext> :
    BaseMulticastHandler<TMessage, TContext>,
    IMulticastContextHandler<TMessage, TContext>,
    IMulticastMessageHandler<TMessage>,
    IMessageHandlerRegistrar<TMessage, TContext>
    where TContext : IMessageContext<TMessage>, new()
{
    public HandlingResultPromise TryHandle(TContext context) =>
        _dispatchPipeline(context).ToPromise();

    public HandlingResultPromise TryHandle(TMessage message) =>
        _dispatchPipeline(new TContext()
        {
            Message = message,
        }).ToPromise();

    public HandlingResultAwaitable TryHandleAsync(TContext context) =>
        _dispatchPipeline(context);

    public HandlingResultAwaitable TryHandleAsync(TMessage message,
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
        return AddRegistration(handler, settings);
    }

    IRegisteredHandler<TMessage, TContext> IMessageHandlerRegistrar<TMessage, TContext>.Register(
        IMessageHandler<TMessage> handler,
        HandlerRegistrationSettings settings)
    {
        return AddRegistration(handler, settings);
    }

    IRegisteredHandler<TMessage> IMessageHandlerRegistrar<TMessage>.Register(
        IMessageHandler<TMessage> handler,
        HandlerRegistrationSettings settings)
    {
        return AddRegistration(handler, settings);
    }

    void IContextHandlerRegistrar<TMessage, TContext>.Unregister(
        IRegisteredHandler registration) => RemoveRegistration(registration);

    void IMessageHandlerRegistrar<TMessage>.Unregister(
        IRegisteredHandler registration) => RemoveRegistration(registration);
}
