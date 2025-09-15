using Chopsticks.Messages.Handlers;
using Chopsticks.Messages.Interceptors;
using Chopsticks.Messages.Registration.Handlers;
using System.Collections.Generic;

namespace Chopsticks.Messages.Registration;

public abstract class ContextHandlerRegistrar<TMessage, TContext> :
    BaseMessageHandlerRegistrar<TMessage, TContext>,
    IContextHandlerRegistrar<TMessage, TContext>
    where TContext : IMessageContext<TMessage>, new()
{
    // TODO :: Support registering interceptors for the multicast.
    // TODO :: Rebuild immutable collection used during handling on any register/unregister.
    // TODO :: Rebuild already registered handlers for intercetpor changes.
    protected IInterceptor<TMessage, TContext>[] MulticastContextInterceptors =>
        [.. _multicastContextInterceptors];
    private readonly List<IInterceptor<TMessage, TContext>> _multicastContextInterceptors = [];

    protected IInterceptor<TMessage, TContext>[] PerHandlerContextInterceptors =>
        [.. _perHandlerContextInterceptors];
    private readonly List<IInterceptor<TMessage, TContext>> _perHandlerContextInterceptors = [];


    public bool Register(IContextHandler<TMessage, TContext> handler, 
        HandlerRegistrationSettings settings, 
        params IInterceptor<TMessage, TContext>[] interceptors)
    {
        // TODO :: Add interceptors to the handler if needed.
        return AddRegistration(new RegisteredContextHandler<TMessage, TContext>(handler)
        {
            Order = settings.Order
        });
    }

    public bool Register(IMessageHandler<TMessage> handler, HandlerRegistrationSettings settings)
    {
        // TODO :: Add interceptors to the handler if needed.
        return AddRegistration(new RegisteredMessageHandler<TMessage, TContext>(handler)
        {
            Order = settings.Order
        });
    }

    public void Unregister(IContextHandler<TMessage, TContext> handler)
    {
        // TODO :: Optimize with an internal registration ID.
        RemoveRegistration(new RegisteredContextHandler<TMessage, TContext>(handler));
    }

    public void Unregister(IMessageHandler<TMessage> handler)
    {
        RemoveRegistration(new RegisteredMessageHandler<TMessage, TContext>(handler));
    }
}
