using System.Threading;

namespace Chopsticks.Messages.Handlers;

public interface IContextHandler<TMessage, TContext> : IMessageHandler<TMessage>
    where TContext : IMessageContext<TMessage>, new()
{

    // TODO :: Add non-try methods.

    HandlingPromise IMessageHandler<TMessage>.TryHandle(TMessage message) =>
        TryHandle(new TContext() 
        { 
            CancellationToken = default,
            Message = message 
        });

    HandlingPromise TryHandle(TContext context);

    
    HandlingAwaitable IMessageHandler<TMessage>.TryHandleAsync(
        TMessage message, CancellationToken token) => 
        TryHandleAsync(new TContext()
        { 
            CancellationToken = token, 
            Message = message 
        });

    HandlingAwaitable TryHandleAsync(TContext context);
}