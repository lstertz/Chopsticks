using Chopsticks.Messages.Handlers;
using Chopsticks.Messages.Registration.Interceptors;

namespace Chopsticks.Messages.Registration.Handlers;

public interface IRegisteredHandler<TMessage, TContext> : 
    IHandlerRegistration
    where TContext : IMessageContext<TMessage>, new()
{
    int Order { get; }

    HandlingAwaitable HandleAsync(TContext context);

    void RebuildHandlePipeline(
        RegisteredInterceptor<TMessage, TContext>[] interceptors);
}
