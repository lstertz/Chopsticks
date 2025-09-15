using Chopsticks.Messages.Handlers;
using Chopsticks.Messages.Interceptors;
using Chopsticks.Messages.Registration.Handlers;
using Chopsticks.Messages.Registration.Interceptors;
using System.Collections.Generic;

namespace Chopsticks.Messages.Registration;

public abstract class BaseMessageHandlerRegistrar<TMessage, TContext>
    where TContext : IMessageContext<TMessage>, new()
{
    private readonly List<RegisteredInterceptor<TMessage, TContext>> _dispatchInterceptors = [];

    // TODO :: Support per-handler interceptors.
    //protected RegisteredInterceptor<TMessage, TContext>[] PerHandlerMessageInterceptors =>
    //    [.. _perHandlerMessageInterceptors];
    //private readonly List<RegisteredInterceptor<TMessage, TContext>> _perHandlerMessageInterceptors = [];

    // TODO :: Rebuild immutable collection used during handling on any register/unregister.
    protected IRegisteredHandler<TMessage, TContext>[] RegisteredMessageHandlers => [.. _registeredMessageHandlers];
    private readonly List<IRegisteredHandler<TMessage, TContext>> _registeredMessageHandlers = new(8);


    public BaseMessageHandlerRegistrar()
    {
        RebuildDispatchInterceptorPipeline(_dispatchInterceptors);
    }


    public void AddDispatchInterceptor(IInterceptor<TMessage, TContext> interceptor, 
        InterceptorRegistrationSettings settings = default)
    {
        var registration = new RegisteredInterceptor<TMessage, TContext>(interceptor)
        {
            Order = settings.Order
        };

        _dispatchInterceptors.Add(registration);
        _dispatchInterceptors.Sort((x, y) =>
        {
            int orderComparison = x.Order.CompareTo(y.Order);
            if (orderComparison != 0)
                return orderComparison;
            return 1;
        });

        RebuildDispatchInterceptorPipeline(_dispatchInterceptors);
    }

    public void RemoveDispatchInterceptor(IInterceptor<TMessage, TContext> interceptor)
    {
        var registration = new RegisteredInterceptor<TMessage, TContext>(interceptor);

        _dispatchInterceptors.Remove(registration);
        RebuildDispatchInterceptorPipeline(_dispatchInterceptors);
    }


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

    protected abstract void RebuildDispatchInterceptorPipeline(
        List<RegisteredInterceptor<TMessage, TContext>> interceptors);
}
