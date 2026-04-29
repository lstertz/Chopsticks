using Chopsticks.Messages.Handlers;
using Chopsticks.Messages.Interceptors;
using Chopsticks.Messages.Interceptors.Adapters;
using Chopsticks.Messages.Registration.Interceptors;
using System;
using System.Collections.Generic;

namespace Chopsticks.Messages.Registration.Handlers;

public abstract class BaseRegisteredHandler<TMessage, TContext> : 
    IRegisteredHandler<TMessage, TContext>
    where TContext : IMessageContext<TMessage>, new()
{
    // TODO :: Support per-handler interceptors, set from the multicaster, triggers rebuild.
    // public RegisteredInterceptor<TMessage, TContext>[] AdditionalInterceptors {get; set; } = [];

    /// <summary>
    /// The order in which this handler will be performed, relative to 
    /// the order of any other handlers registered to the same registrar.
    /// </summary>
    public int Order { get; init; } = 0;

    /// <summary>
    /// The index in which this handler was registered. This is used 
    /// to implicitly order registrations in the order in which they were registered, if 
    /// there is no explicit order (per <see cref="Order"/>) specified.
    /// </summary>
    public int RegistrationIndex { get; init; } = 0;

    private readonly List<RegisteredInterceptor<TMessage, TContext>> _interceptors = [];
    private Func<TContext, HandlingResultAwaitable> _handlePipeline;

    private int _nextRegistrationIndex = 0;


    public BaseRegisteredHandler()
    {
        _handlePipeline = InternalHandleAsync;
    }

    public IRegisteredHandler<TMessage, TContext> AddInterceptor(
        IInterceptor interceptor, 
        InterceptorRegistrationSettings settings = default)
    {
        var adapter = new InterceptorAdapter<TMessage, TContext>(interceptor);
        return AddInterceptor(adapter, settings);
    }

    public IRegisteredHandler<TMessage, TContext> AddInterceptor(
        IMessageInterceptor<TMessage> interceptor, 
        InterceptorRegistrationSettings settings = default)
    {
        var adapter = new MessageInterceptorAdapter<TMessage, TContext>(interceptor);
        return AddInterceptor(adapter, settings);
    }

    public IRegisteredHandler<TMessage, TContext> AddInterceptor<TContract>(
        IContractInterceptor<TContract> interceptor, 
        ContractInterceptorMode mode = ContractInterceptorMode.Required, 
        InterceptorRegistrationSettings settings = default)
    {
        var adapter = new ContractInterceptorAdapter<TMessage, TContext, TContract>(
            interceptor, mode);
        return AddInterceptor(adapter, settings);
    }

    public IRegisteredHandler<TMessage, TContext> AddInterceptor(
        IContextInterceptor<TMessage, TContext> interceptor, 
        InterceptorRegistrationSettings settings = default)
    {
        var registration = new RegisteredInterceptor<TMessage, TContext>(interceptor)
        {
            Order = settings.Order,
            RegistrationIndex = _nextRegistrationIndex++
        };

        _interceptors.Add(registration);
        _interceptors.Sort((x, y) =>
        {
            int orderComparison = x.Order.CompareTo(y.Order);
            if (orderComparison != 0)
                return orderComparison;
            return x.RegistrationIndex.CompareTo(y.RegistrationIndex);
        });

        RebuildHandlePipeline();
        return this;
    }

    IRegisteredHandler<TMessage> IRegisteredHandler<TMessage>.AddInterceptor(
        IInterceptor interceptor,
        InterceptorRegistrationSettings settings) => 
            AddInterceptor(interceptor, settings);

    IRegisteredHandler<TMessage> IRegisteredHandler<TMessage>.AddInterceptor(
        IMessageInterceptor<TMessage> interceptor,
        InterceptorRegistrationSettings settings) => 
            AddInterceptor(interceptor, settings);

    IRegisteredHandler<TMessage> IRegisteredHandler<TMessage>.AddInterceptor<TContract>(
        IContractInterceptor<TContract> interceptor,
        ContractInterceptorMode mode,
        InterceptorRegistrationSettings settings) => 
            AddInterceptor(interceptor, mode, settings);


    public HandlingResultAwaitable HandleAsync(TContext context) =>
        _handlePipeline(context);

    protected abstract HandlingResultAwaitable InternalHandleAsync(TContext context);

    private void RebuildHandlePipeline()
    {
        /*
        List<RegisteredInterceptor<TMessage, TContext>> allInterceptors = 
            [.. _interceptors, .. AdditionalInterceptors];
        allInterceptors.Sort((x, y) =>
        {
            int orderComparison = x.Order.CompareTo(y.Order);
            if (orderComparison != 0)
                return orderComparison;
            return x.RegistrationIndex.CompareTo(y.RegistrationIndex);
        });
        */

        Func<TContext, HandlingResultAwaitable> current = InternalHandleAsync;

        for (int c = _interceptors.Count - 1; c >= 0; c--)
        {
            var next = current;
            var interceptor = _interceptors[c];
            current = (context) =>
                interceptor.InterceptAsync(context, next);
        }

        _handlePipeline = current;
    }
}
