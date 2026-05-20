using System.Threading;
using Chopsticks.Messages.Handlers.Sources;

namespace Chopsticks.Messages.Handlers;

public interface IContextHandler<TMessage, TContext> : IMessageHandler<TMessage>
    where TContext : IMessageContext<TMessage>, new()
{
    /// <summary>
    /// Initiates handling of the provided context, 
    /// returning a promise that completes when the handling is complete. 
    /// The returned promise will throw for any failure or cancellation, 
    /// and it will provide a result that can be checked for whether the context was handled.
    /// </summary>
    /// <remarks>
    /// <see cref="HandlingCompletionPromise.Release"/> must be called when the promise is 
    /// no longer needed to ensure that the underlying source can be reused.
    /// Synchronous completion will be handled by the calling thread, so any exceptions thrown 
    /// by synchronous handling will be thrown directly by this method.
    /// </remarks>
    /// <param name="context">The context to be handled.</param>
    /// <param name="asyncContext">The synchronization context that any async 
    /// exceptions will be posted to.</param>
    /// <returns>A promise to bind continuations to for completion 
    /// and other scenarios.</returns>
    HandlingCompletionPromise Handle(TContext context,
        SynchronizationContext? asyncContext = null) => 
        new(TryHandle(context).Source, asyncContext ?? SynchronizationContext.Current);

    HandlingCompletionAwaitable HandleAsync(TContext context)
    {
        var awaitable = TryHandleAsync(context);
        var source = HandleAsyncPromiseSource.Pool.Rent();
        source.Init(awaitable.Source);

        return new HandlingCompletionAwaitable(source);
    }

    HandlingResultPromise IMessageHandler<TMessage>.TryHandle(TMessage message) =>
        TryHandle(new TContext()
        {
            CancellationToken = default,
            Message = message
        });

    HandlingResultPromise TryHandle(TContext context);


    HandlingResultAwaitable IMessageHandler<TMessage>.TryHandleAsync(
        TMessage message, CancellationToken token) =>

        TryHandleAsync(new TContext()
        {
            CancellationToken = token,
            Message = message
        });

    HandlingResultAwaitable TryHandleAsync(TContext context);
}