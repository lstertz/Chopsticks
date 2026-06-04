using Chopsticks.Messages.Handlers.Sources;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Chopsticks.Messages.Handlers
{
    /// <summary>
    /// A message handler that returns <see cref="ValueTask"/> for optimal performance
    /// when handlers complete synchronously most of the time.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use this interface when your handler often completes synchronously but occasionally
    /// needs to perform async operations. <see cref="ValueTask"/> avoids allocations
    /// for synchronous completions, unlike <see cref="Task"/>.
    /// </para>
    /// <para>
    /// For handlers that always complete synchronously, use <see cref="ISyncMessageHandler{TMessage}"/>.
    /// For handlers that always require async operations, use <see cref="ITaskMessageHandler{TMessage}"/>.
    /// </para>
    /// </remarks>
    /// <typeparam name="TMessage">The type of message this handler processes.</typeparam>
    public interface IValueTaskMessageHandler<TMessage> :
        IMessageHandler<TMessage>
    {
        /// <summary>
        /// Handles the message asynchronously, returning a <see cref="ValueTask"/>.
        /// </summary>
        /// <param name="message">The message to handle.</param>
        /// <param name="token">A cancellation token to observe.</param>
        /// <returns>A <see cref="ValueTask"/> representing the async operation.</returns>
        new ValueTask HandleAsync(TMessage message, CancellationToken token = default);


        HandlingResultPromise IMessageHandler<TMessage>.TryHandle(TMessage message)
        {
            ValueTask valueTask;
            try
            {
                valueTask = HandleAsync(message);
            }
            catch (OperationCanceledException)
            {
                return new HandlingResultPromise(HandlingResult.Cancelled);
            }
            catch (Exception ex)
            {
                return new HandlingResultPromise(HandlingResult.FromException(ex));
            }
            
            // Fast path: if completed synchronously, return direct result (zero allocation)
            if (valueTask.IsCompletedSuccessfully)
            {
                return new HandlingResultPromise(HandlingResult.Success);
            }
            
            // Check for synchronous failure/cancellation (completed but faulted)
            if (valueTask.IsCompleted)
            {
                try
                {
                    valueTask.GetAwaiter().GetResult();
                    return new HandlingResultPromise(HandlingResult.Success);
                }
                catch (OperationCanceledException)
                {
                    return new HandlingResultPromise(HandlingResult.Cancelled);
                }
                catch (Exception ex)
                {
                    return new HandlingResultPromise(HandlingResult.FromException(ex));
                }
            }
            
            // Async path: need to wrap in promise source
            var source = TryHandleAsyncPromiseSource.Rent();
            source.Init(valueTask.AsTask().GetAwaiter());

            return new HandlingResultPromise(source);
        }

        HandlingResultAwaitable IMessageHandler<TMessage>.TryHandleAsync(
            TMessage message, CancellationToken token)
        {
            ValueTask valueTask;
            try
            {
                valueTask = (this as IValueTaskMessageHandler<TMessage>)!.HandleAsync(message, token);
            }
            catch (OperationCanceledException)
            {
                return new HandlingResultAwaitable(HandlingResult.Cancelled);
            }
            catch (Exception ex)
            {
                return new HandlingResultAwaitable(HandlingResult.FromException(ex));
            }
            
            // Fast path: if completed synchronously, return direct result (zero allocation)
            if (valueTask.IsCompletedSuccessfully)
            {
                return new HandlingResultAwaitable(HandlingResult.Success);
            }
            
            // Check for synchronous failure/cancellation
            if (valueTask.IsCompleted)
            {
                try
                {
                    valueTask.GetAwaiter().GetResult();
                    return new HandlingResultAwaitable(HandlingResult.Success);
                }
                catch (OperationCanceledException)
                {
                    return new HandlingResultAwaitable(HandlingResult.Cancelled);
                }
                catch (Exception ex)
                {
                    return new HandlingResultAwaitable(HandlingResult.FromException(ex));
                }
            }
            
            // Async path: need to wrap in promise source
            var source = TryHandleAsyncPromiseSource.Rent();
            source.Init(valueTask.AsTask().GetAwaiter());

            return new HandlingResultAwaitable(source);
        }
    }
}
