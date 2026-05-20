using Chopsticks.Messages.Exceptions;
using Chopsticks.Messages.Handlers.Sources;
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
    public struct HandlingCompletionPromise : IDisposable
    {
        private const string DisposalExceptionMessage = 
            "The promise has been released and can no longer be used.";

        private readonly static SourcePool<HandlePromiseSource> Pool = new();

        /// <summary>
        /// Gets the last known completion status of the message handling operation.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown if the source 
        /// of the promise has already been released through <see cref="Release"/> 
        /// and a cached result after completion is not available.</exception>
        public HandlingCompletion Completion
        {
            get
            {
                UpdateResult();
                return (HandlingCompletion)_result.Status;
            }
        }

        private readonly bool HasBeenReleased => 
            _source == null || _source.Version != _sourceVersion;

        private HandlingResult _result = HandlingResult.Processing;
        private HandlePromiseSource? _source;
        private readonly int _sourceVersion;


        /// <summary>
        /// Constructs a new <see cref="HandlingCompletionPromise"/> with the given promise source.
        /// </summary>
        /// <remarks>
        /// Synchronous completion will be handled to the constructing thread, 
        /// so any exceptions thrown by the source will be thrown directly by this constructor.
        /// </remarks>
        /// <param name="resultPromiseSource">The source that the promise will 
        /// provide the result that the promise acts upon.</param>
        /// <param name="asyncContext">The synchronization context that any async 
        /// excpeptions will be posted to.</param>
        public HandlingCompletionPromise(IHandlingPromiseSource resultPromiseSource,
            SynchronizationContext? asyncContext = null)
        {
            _source = Pool.Rent();
            _sourceVersion = _source.Version;
            _source.Init(resultPromiseSource);

            _source.FailureContext = asyncContext;

            if (!_source.IsCompleted)
            {
                _source.OnCompleted(_source.InitiateDefaultContinuations);
            }
            else
            {
                _source.InitiateDefaultContinuations();
                _result = _source.GetResult();

                Pool.Return(_source);
                _source = null;

                // Try to throw directly if this was synchronous.
                _result.ThrowIfFailed();
            }
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
            var s = _source;
            if (s == null)
            {
                // Already released or already consumed.
                _source = null;
                return;
            }

            if (s.Version != _sourceVersion)
            {
                // Already released by another copy of this struct.
                _source = null;
                return;
            }

            _source = null;
            Pool.Return(s);
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
            UpdateResult();

            if (_result.Status == HandlingStatus.Processing)
            {
                // If the source has not completed immediately, then it must be being handled.
                return this;
            }

            _result.ThrowIfNotHandled(customExceptionMessage);
            return this;
        }

        /// <summary>
        /// The continuation performed when the promise has completed, 
        /// meaning that the message has been handled by at least one handler.
        /// </summary>
        /// <param name="whenCompleted">The action to perform when the promise has completed.</param>
        /// <exception cref="ObjectDisposedException">Thrown if the source 
        /// of the promise has already been released through <see cref="Release"/> 
        /// and a cached result after completion is not available.</exception>
        /// <returns>
        /// The current <see cref="HandlingCompletionPromise"/> instance, 
        /// allowing for method chaining.
        /// </returns>
        public HandlingCompletionPromise WhenCompleted(Action whenCompleted)
        {
            UpdateResult();

            if (_result.Status == HandlingStatus.Processing)
            {
                _source!.OnCompletion = whenCompleted;

                return this;
            }

            if ((_result.Status & HandlingStatus.Completed) != 0)
                whenCompleted();

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
            UpdateResult();

            if (_result.Status == HandlingStatus.Processing)
            {
                // If the source has not completed immediately, then it must be being handled.
                return this;
            }

            if (_result.Status == HandlingStatus.NotHandled)
                whenNotHandled();

            return this;
        }


        private void UpdateResult()
        {
            if (_result.Status != HandlingStatus.Processing)
                return;

            if (HasBeenReleased)
            {
                throw new ObjectDisposedException(nameof(HandlingCompletionPromise),
                    DisposalExceptionMessage);
            }

            var source = _source!;
            if (!source.IsCompleted)
                return;

            _result = source.GetResult();
        }
    }
}
