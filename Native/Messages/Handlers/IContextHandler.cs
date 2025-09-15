using System.Threading;

namespace Chopsticks.Messages.Handlers;

public interface IContextHandler<TMessage, TContext> : IMessageHandler<TMessage>
    where TContext : IMessageContext<TMessage>, new()
{
    HandlingPromise IMessageHandler<TMessage>.Handle(TMessage message) =>
        Handle(new TContext() 
        { 
            CancellationToken = default,
            Message = message 
        });

    HandlingPromise Handle(TContext context);

    
    HandlingAwaitable IMessageHandler<TMessage>.HandleAsync(
        TMessage message, CancellationToken token) => 
        HandleAsync(new TContext()
        { 
            CancellationToken = token, 
            Message = message 
        });

    HandlingAwaitable HandleAsync(TContext context);
}