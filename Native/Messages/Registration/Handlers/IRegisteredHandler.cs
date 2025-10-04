using Chopsticks.Messages.Handlers;
using Chopsticks.Messages.Registration.Interceptors;
using System;

namespace Chopsticks.Messages.Registration.Handlers;


public interface IRegisteredHandler : IDisposable
{
}

public interface IRegisteredHandler<TMessage, TContext> : 
    IRegisteredHandler
    where TContext : IMessageContext<TMessage>, new()
{
    int Order { get; }

    HandlingAwaitable HandleAsync(TContext context);

    void RebuildHandlePipeline(
        RegisteredInterceptor<TMessage, TContext>[] interceptors);
}
