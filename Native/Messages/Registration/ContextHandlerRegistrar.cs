using Chopsticks.Messages.Handlers;
using Chopsticks.Messages.Interceptors;
using System;
using System.Collections.Generic;

namespace Chopsticks.Messages.Registration;

public abstract class ContextHandlerRegistrar<TMessage, TContext> :
    MessageHandlerRegistrar<TMessage>,
    IContextHandlerRegistrar<TMessage, TContext>
    where TContext : IMessageContext<TMessage>
{
    protected class ContextHandlerRegistration : IEquatable<ContextHandlerRegistration>
    {
        public IContextInterceptor<TMessage, TContext>[] Interceptors { get; init; }

        public int Order { get; init; } = 0;

        public IContextHandler<TMessage, TContext> Handler { get; init; }


        public ContextHandlerRegistration(IContextHandler<TMessage, TContext> handler,
            params IContextInterceptor<TMessage, TContext>[] interceptors)
        {
            Handler = handler ?? throw new ArgumentNullException(nameof(handler));
            Interceptors = interceptors ?? [];
        }

        /// <inheritdoc/>
        public bool Equals(ContextHandlerRegistration other) =>
            Handler.Equals(other.Handler);

        /// <inheritdoc/>
        public override int GetHashCode() =>
            Handler.GetHashCode();
    }


    // TODO :: Support registering interceptors for the multicast.
    // TODO :: Rebuild immutable collection used during handling on any register/unregister.
    protected IContextInterceptor<TMessage, TContext>[] MulticastContextInterceptors =>
        [.. _multicastContextInterceptors];
    private readonly List<IContextInterceptor<TMessage, TContext>> _multicastContextInterceptors = [];

    protected IContextInterceptor<TMessage, TContext>[] PerHandlerContextInterceptors =>
        [.. _perHandlerContextInterceptors];
    private readonly List<IContextInterceptor<TMessage, TContext>> _perHandlerContextInterceptors = [];

    // TODO :: Rebuild immutable collection used during handling on any register/unregister.
    protected ContextHandlerRegistration[] RegisteredContextHandlers => [.. _registeredContextHandlers];
    private readonly List<ContextHandlerRegistration> _registeredContextHandlers = new(8);


    // TODO :: Rebuild immutable collection used during handling on any register/unregister.
    bool IContextHandlerRegistrar<TMessage, TContext>.Register(
        IContextHandler<TMessage, TContext> handler,
        RegistrationSettings settings, 
        params IContextInterceptor<TMessage, TContext>[] interceptors)
    {
        var registration = new ContextHandlerRegistration(handler, interceptors)
        {
            Order = settings.Order
        };

        if (_registeredContextHandlers.Contains(registration))
            return false;

        _registeredContextHandlers.Add(registration);
        _registeredContextHandlers.Sort((x, y) =>
        {
            int orderComparison = x.Order.CompareTo(y.Order);
            if (orderComparison != 0)
                return orderComparison;

            return x.Handler.GetHashCode().CompareTo(y.Handler.GetHashCode());
        });

        return true;
    }

    void IContextHandlerRegistrar<TMessage, TContext>.Unregister(
        IContextHandler<TMessage, TContext> handler)
    {
        _registeredContextHandlers.Remove(new(handler));
    }
}
