using Chopsticks.Messages.Exceptions;
using Chopsticks.Messages.Handlers.Sources.Pooling;
using System;
using System.Threading;

namespace Chopsticks.Messages
{
    /// <summary>
    /// A promise representing the completion of a non-try message handling operation.
    /// </summary>
    /// <remarks>
    /// This is a reduced API compared to <see cref="HandlingResultPromise"/>. 
    /// Failure and cancellation states are thrown as exceptions by the underlying 
    /// source, so only <see cref="HandlingCompletion.Successful"/> and 
    /// <see cref="HandlingCompletion.NotHandled"/> are observable.
    /// </remarks>
    public readonly struct HandlingCompletionPromise : IDisposable
    {
        private const string DisposalExceptionMessage = 
            "The promise has been released and can no longer be used.";


        private static readonly SendOrPostCallback ThrowIfFailed = 
            state => ((HandlingResult)state!).ThrowIfFailed();
        private static readonly SendOrPostCallback ThrowIfCancelled = 
            state => ((HandlingResult)state!).ThrowIfCancelled();

        /// <summary>
        /// Gets the last known completion status of the message handling operation.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown if the source 
        /// of the promise has already been released through <see cref="Release"/> 
        /// and a cached result after completion is not available.</exception>
        public HandlingCompletion Completion => 
            (HandlingCompletion)GetResult().Status;


        private bool HasBeenReleased =>
            _awaiter != null && _awaiter.Version != _awaiterVersion;

        private readonly HandlingResult _result = HandlingResult.Processing;

        private readonly IHandlingAwaiter? _awaiter;
        private readonly int _awaiterVersion;


        public HandlingCompletionPromise(HandlingResult result, 
            SynchronizationContext? asyncContext = null)
        {
            if (result.Status == HandlingStatus.Processing)
                throw new ArgumentException("A pre-completed promise cannot have a " +
                    "result that is \"Processing\".", nameof(result));

            _result = result;
            _awaiter = null;

            EnforceThrows(result, asyncContext);
        }

        public HandlingCompletionPromise(IHandlingAwaiter resultAwaiter,
            SynchronizationContext? asyncContext = null)
        {
            if (resultAwaiter.IsCompleted)  // Async awaitable has completed synchronously.
            {
                _result = resultAwaiter.GetResult();
                resultAwaiter.CanRelease = true;

                _awaiter = null;

                EnforceThrows(_result, asyncContext);

                return;
            }

            _awaiter = resultAwaiter;
            _awaiterVersion = resultAwaiter.Version;

            _awaiter.FailureContext = asyncContext ?? SynchronizationContext.Current;
        }

        /// <inheritdoc/>
        void IDisposable.Dispose() => Release();

        /// <summary>
        /// Releases this promise by returning its inner source to a pool. 
        /// This should be done once all operations on all copies of this struct 
        /// have been completed.
        /// </summary>
        /// <remarks>
        /// Any further operations (e.g., continuation calls or use of <see cref="Completion"/>) 
        /// will throw an <see cref="ObjectDisposedException"/>.
        /// </remarks>
        public void Release()
        {
            if (_awaiter != null)
                _awaiter.CanRelease = true;
        }


        /// <summary>
        /// Throws a <see cref="MessageNotHandledException"/> if the message was not 
        /// handled by any handlers.
        /// </summary>
        /// <param name="customExceptionMessage">
        /// The optional message to override the default exception message.
        /// </param>
        /// <exception cref="MessageNotHandledException">
        /// Thrown if the message was not handled.
        /// </exception>
        /// <exception cref="ObjectDisposedException">Thrown if the source 
        /// of the promise has already been released through <see cref="Release"/> 
        /// and a cached result after completion is not available.</exception>
        /// <returns>
        /// The current <see cref="HandlingCompletionPromise"/> instance, 
        /// allowing for method chaining.
        /// </returns>
        public HandlingCompletionPromise ThrowIfNotHandled(
            string? customExceptionMessage = null)
        {
            var status = GetResult().Status;
            if (status == HandlingStatus.Processing)
            {
                // If we don't have a completed result, then it must be being handled.
                return this;
            }
            
            _result.ThrowIfNotHandled(customExceptionMessage);

            return this;
        }

        /// <summary>
        /// The continuation performed when the promise has completed successfully, 
        /// meaning that the message has been handled by at least one handler.
        /// </summary>
        /// <param name="whenCompleted">The action to perform when the promise 
        /// has completed successfully.</param>
        /// <exception cref="ObjectDisposedException">Thrown if the source 
        /// of the promise has already been released through <see cref="Release"/> 
        /// and a cached result after completion is not available.</exception>
        /// <returns>
        /// The current <see cref="HandlingCompletionPromise"/> instance, 
        /// allowing for method chaining.
        /// </returns>
        public HandlingCompletionPromise WhenCompleted(Action whenCompleted)
        {
            var result = GetResult();
            if (result.Status != HandlingStatus.Processing)
            {
                if ((result.Status & HandlingStatus.Completed) != 0)
                    whenCompleted();
                return this;
            }

            _awaiter!.OnCompletion = whenCompleted;
            return this;
        }

        /// <summary>
        /// The continuation performed when the promise was not completed 
        /// due to the  message not being handled.
        /// </summary>
        /// <param name="whenNotHandled">The action to perform when the message was not handled.</param>
        /// <remarks>
        /// The continuation will be performed immediately (synchronously) if the 
        /// message was not handled.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">Thrown if the source 
        /// of the promise has already been released through <see cref="Release"/> 
        /// and a cached result after completion is not available.</exception>
        /// <returns>
        /// The current <see cref="HandlingCompletionPromise"/> instance, 
        /// allowing for method chaining.
        /// </returns>
        public HandlingCompletionPromise WhenNotHandled(Action whenNotHandled)
        {
            var status = GetResult().Status;
            if (status == HandlingStatus.Processing)
            {
                // If we don't have a completed result, then it must be being handled.
                return this;
            }

            if (status == HandlingStatus.NotHandled)
                whenNotHandled();

            return this;
        }


        private void EnforceThrows(HandlingResult result,
            SynchronizationContext? asyncContext = null)
        {
            var context = asyncContext ?? SynchronizationContext.Current;

            if (_result.Status == HandlingStatus.Failure)
                context?.Post(ThrowIfFailed, _result);
            else if (_result.Status == HandlingStatus.Cancelled)
                context?.Post(ThrowIfCancelled, _result);
        }

        private HandlingResult GetResult()
        {
            if (_awaiter == null)
                return _result;

            if (HasBeenReleased)
            {
                throw new ObjectDisposedException(nameof(HandlingCompletionPromise),
                    DisposalExceptionMessage);
            }

            return _awaiter.GetResult();
        }
    }
}
