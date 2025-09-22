using Chopsticks.Messages.Handlers;
using Chopsticks.Messages.Registration.Interceptors;
using System;

namespace Chopsticks.Messages.Registration.Handlers;

public abstract class BaseRegisteredHandler<TMessage, TContext> : 
    IRegisteredHandler<TMessage, TContext>
    where TContext : IMessageContext<TMessage>, new()
{
    public int Order { get; init; } = 0;

    public RegisteredInterceptor<TMessage, TContext>[] Interceptors { get; init; } = [];
    private Func<TContext, HandlingAwaitable>? _handlePipeline;


    public HandlingAwaitable HandleAsync(TContext context) =>
        _handlePipeline!(context);

    public void RebuildHandlePipeline(
        RegisteredInterceptor<TMessage, TContext>[] interceptors)
    {
        // TODO :: Build the pipeline by combining the interceptors wrapping InternalHandleAsync.

        _handlePipeline = InternalHandleAsync;
    }


    protected abstract HandlingAwaitable InternalHandleAsync(TContext context);
}
