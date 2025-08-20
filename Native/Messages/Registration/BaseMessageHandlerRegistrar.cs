using Chopsticks.Messages.Handlers;
using Chopsticks.Messages.Interceptors;
using Chopsticks.Messages.Registration.Handlers;
using System.Collections.Generic;

namespace Chopsticks.Messages.Registration;

public abstract class BaseMessageHandlerRegistrar<TMessage, TContext>
    where TContext : IMessageContext<TMessage>, new()
{
    private readonly List<IMessageInterceptor<TMessage>> _messageDispatchInterceptors = [];

    protected IMessageInterceptor<TMessage>[] PerHandlerMessageInterceptors =>
        [.. _perHandlerMessageInterceptors];
    private readonly List<IMessageInterceptor<TMessage>> _perHandlerMessageInterceptors = [];

    // TODO :: Rebuild immutable collection used during handling on any register/unregister.
    protected IRegisteredHandler<TMessage, TContext>[] RegisteredMessageHandlers => [.. _registeredMessageHandlers];
    private readonly List<IRegisteredHandler<TMessage, TContext>> _registeredMessageHandlers = new(8);


    public BaseMessageHandlerRegistrar()
    {
        RebuildDispatchInterceptorPipeline(_messageDispatchInterceptors);
    }


    public void AddDispatchInterceptor(IMessageInterceptor<TMessage> interceptor, 
        RegistrationSettings settings = default)  // TODO :: Split interceptor and registration settings.
    {
        _messageDispatchInterceptors.Add(interceptor);

        // TODO :: Wrap interceptors in registrations to include registration settings.
        // TODO :: Support ordering of interceptors.
        //_multicastMessageInterceptors.Sort((x, y) =>
        //{
        //    int orderComparison = x.Order.CompareTo(y.Order);
        //    if (orderComparison != 0)
        //        return orderComparison;
        //    return 1;
        //});

        RebuildDispatchInterceptorPipeline(_messageDispatchInterceptors);
    }

    public void RemoveDispatchInterceptor(IMessageInterceptor<TMessage> interceptor)
    {
        _messageDispatchInterceptors.Remove(interceptor);
        RebuildDispatchInterceptorPipeline(_messageDispatchInterceptors);
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
        List<IMessageInterceptor<TMessage>> interceptors);
}
