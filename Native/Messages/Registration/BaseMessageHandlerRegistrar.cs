using Chopsticks.Messages.Handlers;
using Chopsticks.Messages.Interceptors;
using Chopsticks.Messages.Registration.Handlers;
using System.Collections.Generic;

namespace Chopsticks.Messages.Registration;

public abstract class BaseMessageHandlerRegistrar<TMessage, TContext>
    where TContext : IMessageContext<TMessage>, new()
{
    // TODO :: Support registering interceptors for the multicast.
    // TODO :: Rebuild immutable collection used during handling on any register/unregister.
    protected IMessageInterceptor<TMessage>[] MulticastMessageInterceptors =>
        [.. _multicastMessageInterceptors];
    private readonly List<IMessageInterceptor<TMessage>> _multicastMessageInterceptors = [];

    protected IMessageInterceptor<TMessage>[] PerHandlerMessageInterceptors =>
        [.. _perHandlerMessageInterceptors];
    private readonly List<IMessageInterceptor<TMessage>> _perHandlerMessageInterceptors = [];

    // TODO :: Rebuild immutable collection used during handling on any register/unregister.
    protected IRegisteredHandler<TMessage, TContext>[] RegisteredMessageHandlers => [.. _registeredMessageHandlers];
    private readonly List<IRegisteredHandler<TMessage, TContext>> _registeredMessageHandlers = new(8);



    // TODO :: Rebuild immutable collection used during handling on any register/unregister.
    protected bool AddRegistration(IRegisteredHandler<TMessage, TContext> registration)
    {
        if (_registeredMessageHandlers.Contains(registration))
            return false;

        _registeredMessageHandlers.Add(registration);
        _registeredMessageHandlers.Sort((x, y) =>
        {
            int orderComparison = x.Order.CompareTo(y.Order);
            if (orderComparison != 0)
                return orderComparison;
            return 1;
        });

        return true;
    }

    protected void RemoveRegistration(IRegisteredHandler<TMessage, TContext> registration)
    {
        _registeredMessageHandlers.Remove(registration);
    }
}
