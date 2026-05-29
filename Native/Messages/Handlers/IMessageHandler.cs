using System;
using System.Threading;
using Chopsticks.Messages.Handlers.Sources;

namespace Chopsticks.Messages.Handlers
{
    public interface IMessageHandler<TMessage>
    {
        HandlingCompletionPromise Handle(TMessage message,
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
            var awaitable = TryHandleAsync(message, default);

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

        HandlingCompletionAwaitable HandleAsync(TMessage message, CancellationToken token = default)
        {
            var awaitable = TryHandleAsync(message, token);
            
            // Fast path: if the result is direct (sync handler), skip wrapping  
            if (awaitable.Source == null)
            {
                var result = awaitable.GetAwaiter().GetResult();
                // Wrap in AggregateException for backward compatibility
                if (result.Status == HandlingStatus.Failure)
                    throw new AggregateException(result.Exceptions);
                if (result.Status == HandlingStatus.Cancelled)
                    throw new OperationCanceledException();
                return new HandlingCompletionAwaitable(result);
            }
            
            var source = HandleAsyncPromiseSource.Rent();
            source.Init(awaitable.Source);

            return new HandlingCompletionAwaitable(source);
        }

        HandlingResultPromise TryHandle(TMessage message);

        HandlingResultAwaitable TryHandleAsync(TMessage message, CancellationToken token = default);
    }
}
