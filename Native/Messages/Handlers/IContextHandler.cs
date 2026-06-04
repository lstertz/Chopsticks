using System.Threading;
using Chopsticks.Messages.Handlers.Sources;

namespace Chopsticks.Messages.Handlers;

public interface IContextHandler<TMessage, TContext> : IMessageHandler<TMessage>
    where TContext : IMessageContext<TMessage>, new()
{
    HandlingCompletionPromise Handle(TContext context,
        SynchronizationContext? asyncContext = null)
    {
        // NOTE: Obtain the source via TryHandleAsync rather than TryHandle. The
        // HandlingResultPromise returned by TryHandle pre-registers the inner source's
        // InitiateDefaultContinuations, which auto-returns the pooled inner source on
        // completion. Wrapping that same source here would register a SECOND continuation,
        // and whichever fires first wins a race: if the inner's own continuation runs first
        // it disposes the source before the wrapper can read its result, yielding a spurious
        // NotHandled and a completion callback that never fires. The awaitable from
        // TryHandleAsync does not pre-register a continuation, leaving the wrapper as the
        // sole owner of the inner source's completion.
        var awaitable = TryHandleAsync(context);

        // Fast path: if the result is direct (sync handler), skip wrapping
        if (awaitable.Source == null)
        {
            var result = awaitable.GetAwaiter().GetResult();
            // Throw raw exception for sync handling (matches original behavior)
            result.ThrowIfFailed();
            return new HandlingCompletionPromise(result);
        }

        var source = HandlePromiseSource.Rent();
        source.Init(awaitable.Source);

        return new HandlingCompletionPromise(source,
            asyncContext ?? SynchronizationContext.Current);
    }

    HandlingCompletionAwaitable HandleAsync(TContext context)
    {
        var awaitable = TryHandleAsync(context);
        
        // Fast path: if the result is direct (sync handler), skip wrapping
        if (awaitable.Source == null)
        {
            var result = awaitable.GetAwaiter().GetResult();
            // Wrap in AggregateException for backward compatibility
            if (result.Status == HandlingStatus.Failure)
                throw new System.AggregateException(result.Exceptions);
            if (result.Status == HandlingStatus.Cancelled)
                throw new System.OperationCanceledException();
            return new HandlingCompletionAwaitable(result);
        }
        
        var source = HandleAsyncPromiseSource.Rent();
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