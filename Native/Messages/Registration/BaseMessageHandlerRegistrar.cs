using Chopsticks.Messages.Handlers;
using Chopsticks.Messages.Interceptors;
using Chopsticks.Messages.Interceptors.Adapters;
using Chopsticks.Messages.Registration.Handlers;
using Chopsticks.Messages.Registration.Interceptors;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Chopsticks.Messages.Registration;

public abstract class BaseMessageHandlerRegistrar<TMessage, TContext>
    where TContext : IMessageContext<TMessage>, new()
{
    private readonly List<RegisteredInterceptor<TMessage, TContext>> _dispatchInterceptors = [];
    private readonly object _interceptorLock = new();

    private static readonly BaseRegisteredHandler<TMessage, TContext>[] EmptyHandlers = 
        Array.Empty<BaseRegisteredHandler<TMessage, TContext>>();
    
    protected BaseRegisteredHandler<TMessage, TContext>[] RegisteredMessageHandlers => 
        Volatile.Read(ref _cachedHandlers);
    
    private BaseRegisteredHandler<TMessage, TContext>[] _cachedHandlers = EmptyHandlers;
    private readonly List<BaseRegisteredHandler<TMessage, TContext>> _registeredMessageHandlers = new(8);
    private readonly object _handlerLock = new();

    private int _nextHandlerRegistrationIndex = 0;
    private int _nextInterceptorRegistrationIndex = 0;


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
        ContractInterceptorMode mode = ContractInterceptorMode.Required,
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
        lock (_interceptorLock)
        {
            var registration = new RegisteredInterceptor<TMessage, TContext>(interceptor)
            {
                Order = settings.Order,
                RegistrationIndex = _nextInterceptorRegistrationIndex++
            };

            int insertIndex = BinarySearchInsertIndex(_dispatchInterceptors, registration);
            _dispatchInterceptors.Insert(insertIndex, registration);

            RebuildDispatchPipeline(_dispatchInterceptors);
            return registration;
        }
    }

    public void RemoveDispatchInterceptor(IRegisteredInterceptor registration)
    {
        if (registration is not RegisteredInterceptor<TMessage, TContext> reg)
            return;

        lock (_interceptorLock)
        {
            if (_dispatchInterceptors.Remove(reg))
                RebuildDispatchPipeline(_dispatchInterceptors);
        }
    }


    protected RegisteredContextHandler<TMessage, TContext> AddRegistration(
        IContextHandler<TMessage, TContext> handler,
        HandlerRegistrationSettings settings)
    {
        var registration = new RegisteredContextHandler<TMessage, TContext>(handler)
        {
            Order = settings.Order,
            RegistrationIndex = _nextHandlerRegistrationIndex++
        };

        AddRegistration(registration);
        return registration;
    }

    protected RegisteredMessageHandler<TMessage, TContext> AddRegistration(
        IMessageHandler<TMessage> handler,
        HandlerRegistrationSettings settings)
    {
        var registration = new RegisteredMessageHandler<TMessage, TContext>(handler)
        {
            Order = settings.Order,
            RegistrationIndex = _nextHandlerRegistrationIndex++
        };

        AddRegistration(registration);
        return registration;
    }

    protected void RemoveRegistration(IRegisteredHandler registration)
    {
        if (registration is not BaseRegisteredHandler<TMessage, TContext> reg)
            return;

        lock (_handlerLock)
        {
            if (_registeredMessageHandlers.Remove(reg))
                RebuildHandlerCache();
        }
    }

    protected abstract void RebuildDispatchPipeline(
        List<RegisteredInterceptor<TMessage, TContext>> interceptors);

    private void AddRegistration(BaseRegisteredHandler<TMessage, TContext> registration)
    {
        lock (_handlerLock)
        {
            if (_registeredMessageHandlers.Contains(registration))
                return;

            int insertIndex = BinarySearchInsertIndex(_registeredMessageHandlers, registration);
            _registeredMessageHandlers.Insert(insertIndex, registration);
            
            RebuildHandlerCache();
        }
    }
    
    private void RebuildHandlerCache()
    {
        var newCache = _registeredMessageHandlers.Count == 0 
            ? EmptyHandlers 
            : _registeredMessageHandlers.ToArray();
        Volatile.Write(ref _cachedHandlers, newCache);
    }
    
    private static int BinarySearchInsertIndex<T>(List<T> list, T item)
        where T : IOrderedRegistration
    {
        int low = 0;
        int high = list.Count - 1;

        while (low <= high)
        {
            int mid = low + ((high - low) >> 1);
            int cmp = CompareRegistrations(list[mid], item);

            if (cmp < 0)
                low = mid + 1;
            else
                high = mid - 1;
        }

        return low;
    }
    
    private static int CompareRegistrations<T>(T x, T y)
        where T : IOrderedRegistration
    {
        int orderComparison = x.Order.CompareTo(y.Order);
        if (orderComparison != 0)
            return orderComparison;
        return x.RegistrationIndex.CompareTo(y.RegistrationIndex);
    }
}
