using Chopsticks.Messages.Handlers;
using Chopsticks.Messages.Registration.Interceptors;
using System;
using System.Collections.Generic;

namespace Chopsticks.Messages.Registration.Handlers;

public abstract class BaseRegisteredHandler<TMessage, TContext> : 
    IRegisteredHandler<TMessage, TContext>
    where TContext : IMessageContext<TMessage>, new()
{
    public int Order { get; init; } = 0;

    public RegisteredInterceptor<TMessage, TContext>[] Interceptors { get; init; } = [];
    private Func<TContext, HandlingAwaitable>? _handlePipeline;


    public void Dispose()
    {
        // TODO :: Automate the unregistration process.
    }

    public HandlingAwaitable HandleAsync(TContext context) =>
        _handlePipeline!(context);

    public void RebuildHandlePipeline(
        RegisteredInterceptor<TMessage, TContext>[] interceptors)
    {
        List<RegisteredInterceptor<TMessage, TContext>> allInterceptors = 
            [.. Interceptors, .. interceptors];
        allInterceptors.Sort((x, y) =>
        {
            int orderComparison = x.Order.CompareTo(y.Order);
            if (orderComparison != 0)
                return orderComparison;
            return 1;
        });

        Func<TContext, HandlingAwaitable> current = InternalHandleAsync;

        for (int c = allInterceptors.Count - 1; c >= 0; c--)
        {
            var next = current;
            var interceptor = allInterceptors[c];
            current = (context) =>
                interceptor.InterceptAsync(context, next);
        }

        _handlePipeline = current;
    }


    protected abstract HandlingAwaitable InternalHandleAsync(TContext context);
}
