using Chopsticks.Messages.Handlers;
using Chopsticks.Messages.Interceptors;
using Chopsticks.Messages.Interceptors.Adapters;
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
    protected BaseRegisteredHandler<TMessage, TContext>[] RegisteredMessageHandlers => [.. _registeredMessageHandlers];
    private readonly List<BaseRegisteredHandler<TMessage, TContext>> _registeredMessageHandlers = new(8);


    public BaseMessageHandlerRegistrar()
    {
        RebuildDispatchPipeline(_dispatchInterceptors);
    }

    public IRegisteredInterceptor AddDispatchInterceptor(
        IInterceptor interceptor,
        InterceptorRegistrationSettings settings = default)
    {
        var adapter = new InterceptorAdapter<TMessage, TContext>(interceptor);
        return AddDispatchInterceptor(adapter, settings);
    }

    public IRegisteredInterceptor AddDispatchInterceptor(
        IMessageInterceptor<TMessage> interceptor,
        InterceptorRegistrationSettings settings = default)
    {
        var adapter = new MessageInterceptorAdapter<TMessage, TContext>(interceptor);
        return AddDispatchInterceptor(adapter, settings);
    }
    public IRegisteredInterceptor AddDispatchInterceptor<TContract>(
        IContractInterceptor<TContract> interceptor,
        ContractInterceptorMode mode,
        InterceptorRegistrationSettings settings = default)
    {
        var adapter = new ContractInterceptorAdapter<TMessage, TContext, TContract>(
            interceptor, mode);
        return AddDispatchInterceptor(adapter, settings);
    }

    public IRegisteredInterceptor AddDispatchInterceptor(
        IContextInterceptor<TMessage, TContext> interceptor,
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

        RebuildDispatchPipeline(_dispatchInterceptors);
        return registration;
    }

    public void RemoveDispatchInterceptor(IRegisteredInterceptor registration)
    {
        if (registration is not RegisteredInterceptor<TMessage, TContext> reg)
            return;

        if (_dispatchInterceptors.Remove(reg))
            RebuildDispatchPipeline(_dispatchInterceptors);
    }


    // TODO :: Rebuild immutable collection used during handling on any register/unregister.
    //             Immutability is needed to avoid locking during message dispatch.
    protected void AddRegistration(BaseRegisteredHandler<TMessage, TContext> registration)
    {
        if (_registeredMessageHandlers.Contains(registration))
            return;

        _registeredMessageHandlers.Add(registration);
        _registeredMessageHandlers.Sort((x, y) =>
        {
            int orderComparison = x.Order.CompareTo(y.Order);
            if (orderComparison != 0)
                return orderComparison;
            return 1;
        });
    }

    protected void RemoveRegistration(IRegisteredHandler registration)
    {
        if (registration is not BaseRegisteredHandler<TMessage, TContext> reg)
            return;

        _registeredMessageHandlers.Remove(reg);
    }

    protected abstract void RebuildDispatchPipeline(
        List<RegisteredInterceptor<TMessage, TContext>> interceptors);
}
