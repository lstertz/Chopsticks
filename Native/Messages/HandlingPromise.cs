using Chopsticks.Messages.Exceptions;
using Chopsticks.Messages.Handlers.Sources;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Chopsticks.Messages
{
    public struct HandlingPromise
    {
        public static HandlingPromise NoHandlers => new(_noHandlersSource);
        private static readonly IHandlingPromiseSource _noHandlersSource = 
            new SyncHandlingPromiseSource().Init(HandlingResult.NoHandlers);

        public static HandlingPromise Success => new(_successSource);
        private static readonly IHandlingPromiseSource _successSource =
            new SyncHandlingPromiseSource().Init(HandlingResult.Success);


        public HandlingStatus Status => _source.IsCompleted ? 
            _source.GetResult().Status : HandlingStatus.Processing;

        private readonly IHandlingPromiseSource _source;


        public HandlingPromise(IHandlingPromiseSource source)
        {
            _source = source;
            if (!_source.IsCompleted)
                _source.OnCompleted(_source.InitiateDefaultContinuations);
        }

        public HandlingPromise OnCancelled(Action onCancelled)
        {
            if (!_source.IsCompleted)
            {
                _source.OnCancelled = onCancelled;
                return this;
            }

            var result = _source.GetResult();
            if (result.Status == HandlingStatus.Cancelled)
                onCancelled();

            return this;
        }

        public HandlingPromise OnCompletion(Action<HandlingResult> onCompletion)
        {
            if (!_source.IsCompleted)
            {
                _source.OnCompletion = onCompletion;
                return this;
            }

            var result = _source.GetResult();
            if ((result.Status & HandlingStatus.Completed) != 0)
                onCompletion(result);

            return this;
        }

        public HandlingPromise OnFailure(Action<IEnumerable<Exception>> onFailure)
        {
            if (!_source.IsCompleted)
            {
                _source.OnFailure = onFailure;
                return this;
            }

            var result = _source.GetResult();
            if (result.Status == HandlingStatus.Failure)
                onFailure(result.Exceptions);

            return this;
        }

        public HandlingPromise OnNonSuccess(Action<HandlingResult> onNonSuccess)
        {
            if (!_source.IsCompleted)
            {
                _source.OnNonSuccess = onNonSuccess;
                return this;
            }

            var result = _source.GetResult();
            if ((result.Status & HandlingStatus.NonSuccess) != 0)
                onNonSuccess(result);

            return this;
        }

        public HandlingPromise OnSuccess(Action onSuccess)
        {
            if (!_source.IsCompleted)
            {
                _source.OnSuccess = onSuccess;
                return this;
            }

            var result = _source.GetResult();
            if (result.Status == HandlingStatus.Success)
                onSuccess();

            return this;
        }

        /// <summary>
        /// Rethrows any exceptions encountered during the handling of the message.
        /// </summary>
        /// <remarks>
        /// This will immediately throw any exceptions in the current synchronization context 
        /// if the handling was performed synchronously and will post any exceptions 
        /// encountered during asynchronous handling to the <paramref name="asyncContext"/> 
        /// (or attempt to post to the current context).
        /// </remarks>
        /// <param name="asyncContext">
        /// The <see cref="SynchronizationContext"/> to use for posting asynchronous exceptions. 
        /// If <paramref name="asyncContext"/> is <see langword="null"/>, the current 
        /// synchronization context will attempt to be used, but in doing so, 
        /// exceptions may be lost.</param>
        /// <returns>
        /// The current <see cref="HandlingPromise"/> instance, 
        /// allowing for method chaining.
        /// </returns>
        public readonly HandlingPromise ThrowIfFailed(SynchronizationContext? asyncContext = null)
        {
            if (!_source.IsCompleted)
            {
                _source.FailureContext = asyncContext ?? SynchronizationContext.Current;
                return this;
            }

            var result = _source.GetResult();
            result.ThrowIfFailed();  // Synchronous handling throws on the current context.
            return this;
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
        /// <returns>
        /// The current <see cref="HandlingPromise"/> instance, 
        /// allowing for method chaining.
        /// </returns>
        public HandlingPromise ThrowIfNotHandled(string? customExceptionMessage = null)
        {
            if (!_source.IsCompleted)
            {
                // If the source has not completed immediately, then it must be being handled.
                return this;
            }

            var result = _source.GetResult();
            result.ThrowIfNotHandled(customExceptionMessage);

            return this;
        }

        public HandlingPromise WhenNotHandled(Action whenNotHandled)
        {
            if (!_source.IsCompleted)
            {
                // If the source has not completed immediately, then it must be being handled.
                return this;
            }

            var result = _source.GetResult();
            if (result.Status == HandlingStatus.NotHandled)
                whenNotHandled();

            return this;
        }
    }
}