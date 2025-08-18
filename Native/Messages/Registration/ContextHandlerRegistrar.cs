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
    protected IContextInterceptor<TMessage, TContext>[] MulticastContextInterceptors =>
        [.. _multicastContextInterceptors];
    private readonly List<IContextInterceptor<TMessage, TContext>> _multicastContextInterceptors = [];

    protected IContextInterceptor<TMessage, TContext>[] PerHandlerContextInterceptors =>
        [.. _perHandlerContextInterceptors];
    private readonly List<IContextInterceptor<TMessage, TContext>> _perHandlerContextInterceptors = [];


    public bool Register(IContextHandler<TMessage, TContext> handler, 
        RegistrationSettings settings, 
        params IContextInterceptor<TMessage, TContext>[] interceptors)
    {
        // TODO :: Add interceptors to the handler if needed.
        return AddRegistration(new RegisteredContextHandler<TMessage, TContext>(handler)
        {
            Order = settings.Order
        });
    }

    public bool Register(IMessageHandler<TMessage> handler, RegistrationSettings settings)
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
