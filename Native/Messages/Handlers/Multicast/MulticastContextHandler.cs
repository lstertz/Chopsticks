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
    
    /// <summary>
    /// Dispatches the message to all handlers without returning a result.
    /// Zero allocation fire-and-forget pattern - exceptions are swallowed.
    /// </summary>
    /// <remarks>
    /// Use this when you don't need the handling result and want optimal performance.
    /// For error handling, use <see cref="HandleSync"/> instead.
    /// </remarks>
    public void TryHandleFireAndForget(TMessage message, CancellationToken token = default) =>
        DispatchFireAndForget(message, token);
    
    /// <summary>
    /// Dispatches the message to all handlers without returning a result.
    /// Zero allocation fire-and-forget pattern - exceptions are swallowed.
    /// </summary>
    public void TryHandleFireAndForget(TContext context) =>
        DispatchFireAndForget(context.Message, context.CancellationToken);
    
    /// <summary>
    /// Dispatches the message to all handlers synchronously and returns the result.
    /// Zero allocation when all handlers are synchronous.
    /// </summary>
    /// <returns>The aggregated handling result from all handlers.</returns>
    public HandlingResult HandleSync(TMessage message, CancellationToken token = default) =>
        DispatchSync(message, token);
    
    /// <summary>
    /// Dispatches the message to all handlers synchronously and returns the result.
    /// Zero allocation when all handlers are synchronous.
    /// </summary>
    public HandlingResult HandleSync(TContext context) =>
        DispatchSync(context.Message, context.CancellationToken);


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

    void IMessageHandlerRegistrar<TMessage>.Unregister(
        IRegisteredHandler registration) => RemoveRegistration(registration);
}
