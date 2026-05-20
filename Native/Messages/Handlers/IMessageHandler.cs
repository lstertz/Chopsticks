using System.Threading;
using Chopsticks.Messages.Handlers.Sources;

namespace Chopsticks.Messages.Handlers
{
    public interface IMessageHandler<TMessage>
    {
        /// <summary>
        /// Initiates handling of the provided message, 
        /// returning a promise that completes when the handling is complete. 
        /// The returned promise will throw for any failure or cancellation, 
        /// and it will provide a result that can be checked for whether the message was handled.
        /// </summary>
        /// <remarks>
        /// <see cref="HandlingCompletionPromise.Release"/> must be called when the promise is 
        /// no longer needed to ensure that the underlying source can be reused.
        /// Synchronous completion will be handled by the calling thread, so any exceptions thrown 
        /// by synchronous handling will be thrown directly by this method.
        /// </remarks>
        /// <param name="message">The message to be handled.</param>
        /// <param name="asyncContext">The synchronization context that any async 
        /// exceptions will be posted to.</param>
        /// <returns>A promise to bind continuations to for completion 
        /// and other scenarios.</returns>
        HandlingCompletionPromise Handle(TMessage message,
            SynchronizationContext? asyncContext = null) =>
            new(TryHandle(message).Source, asyncContext ?? SynchronizationContext.Current);

        HandlingCompletionAwaitable HandleAsync(TMessage message, CancellationToken token = default)
        {
            var awaitable = TryHandleAsync(message, token);
            var source = HandleAsyncPromiseSource.Pool.Rent();
            source.Init(awaitable.Source);

            return new HandlingCompletionAwaitable(source);
        }

        HandlingResultPromise TryHandle(TMessage message);

        HandlingResultAwaitable TryHandleAsync(TMessage message, CancellationToken token = default);
    }
}
