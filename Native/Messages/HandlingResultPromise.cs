using System;
using System.Collections.Generic;
using System.Threading;
using Chopsticks.Messages.Exceptions;
using Chopsticks.Messages.Handlers.Sources;

namespace Chopsticks.Messages
{
    public readonly struct HandlingResultPromise
    {
        public static HandlingResultPromise NoHandlers => new(HandlingResult.NoHandlers);

        public static HandlingResultPromise Success => new(HandlingResult.Success);


        public HandlingStatus Status => _hasDirectResult ? 
            _directResult.Status : 
            (_source?.IsCompleted == true ? _source.GetResult().Status : HandlingStatus.Processing);

        internal IHandlingPromiseSource? Source => _source;
        private readonly IHandlingPromiseSource? _source;
        
        private readonly HandlingResult _directResult;
        private readonly bool _hasDirectResult;
        
        /// <summary>
        /// Gets the result if available. For internal use in Handle() fast path.
        /// </summary>
        internal HandlingResult GetResultIfCompleted() => 
            _hasDirectResult ? _directResult : 
            (_source?.IsCompleted == true ? _source.GetResult() : HandlingResult.NoHandlers);

        /// <summary>
        /// Creates a promise from a direct result. Zero allocation for sync paths.
        /// </summary>
        public HandlingResultPromise(HandlingResult result)
        {
            _directResult = result;
            _hasDirectResult = true;
            _source = null;
        }

        public HandlingResultPromise(IHandlingPromiseSource source)
        {
            _source = source;
            _directResult = default;
            _hasDirectResult = false;
            // Fluent consumption may register callbacks after completion, so the source must not be
            // recycled out from under this promise. Suppress BEFORE registering any continuation.
            _source.SuppressPooling();
            if (!_source.IsCompleted)
                _source.OnCompleted(_source.InitiateDefaultContinuations);
        }

        public HandlingResultPromise OnCancelled(Action onCancelled)
        {
            if (_hasDirectResult)
            {
                if (_directResult.Status == HandlingStatus.Cancelled)
                    onCancelled();
                return this;
            }
            
            if (!_source!.IsCompleted)
            {
                _source.OnCancelled = onCancelled;
                return this;
            }

            var result = _source.GetResult();
            if (result.Status == HandlingStatus.Cancelled)
                onCancelled();

            return this;
        }

        public HandlingResultPromise OnCompletion(Action<HandlingResult> onCompletion)
        {
            if (_hasDirectResult)
            {
                if ((_directResult.Status & HandlingStatus.Completed) != 0)
                    onCompletion(_directResult);
                return this;
            }
            
            if (!_source!.IsCompleted)
            {
                _source.OnCompletion = onCompletion;
                return this;
            }

            var result = _source.GetResult();
            if ((result.Status & HandlingStatus.Completed) != 0)
                onCompletion(result);

            return this;
        }

        public HandlingResultPromise OnFailure(Action<IEnumerable<Exception>> onFailure)
        {
            if (_hasDirectResult)
            {
                if (_directResult.Status == HandlingStatus.Failure)
                    onFailure(_directResult.Exceptions);
                return this;
            }
            
            if (!_source!.IsCompleted)
            {
                _source.OnFailure = onFailure;
                return this;
            }

            var result = _source.GetResult();
            if (result.Status == HandlingStatus.Failure)
                onFailure(result.Exceptions);

            return this;
        }

        public HandlingResultPromise OnNonSuccess(Action<HandlingResult> onNonSuccess)
        {
            if (_hasDirectResult)
            {
                if ((_directResult.Status & HandlingStatus.NonSuccess) != 0)
                    onNonSuccess(_directResult);
                return this;
            }
            
            if (!_source!.IsCompleted)
            {
                _source.OnNonSuccess = onNonSuccess;
                return this;
            }

            var result = _source.GetResult();
            if ((result.Status & HandlingStatus.NonSuccess) != 0)
                onNonSuccess(result);

            return this;
        }

        public HandlingResultPromise OnSuccess(Action onSuccess)
        {
            if (_hasDirectResult)
            {
                if (_directResult.Status == HandlingStatus.Success)
                    onSuccess();
                return this;
            }
            
            if (!_source!.IsCompleted)
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
        /// The current <see cref="HandlingResultPromise"/> instance, 
        /// allowing for method chaining.
        /// </returns>
        public readonly HandlingResultPromise ThrowIfFailed(SynchronizationContext? asyncContext = null)
        {
            if (_hasDirectResult)
            {
                _directResult.ThrowIfFailed();
                return this;
            }
            
            if (!_source!.IsCompleted)
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
        /// The current <see cref="HandlingResultPromise"/> instance, 
        /// allowing for method chaining.
        /// </returns>
        public readonly HandlingResultPromise ThrowIfNotHandled(string? customExceptionMessage = null)
        {
            if (_hasDirectResult)
            {
                _directResult.ThrowIfNotHandled(customExceptionMessage);
                return this;
            }
            
            if (!_source!.IsCompleted)
            {
                // If the source has not completed immediately, then it must be being handled.
                return this;
            }

            var result = _source.GetResult();
            result.ThrowIfNotHandled(customExceptionMessage);

            return this;
        }

        public HandlingResultPromise WhenNotHandled(Action whenNotHandled)
        {
            if (_hasDirectResult)
            {
                if (_directResult.Status == HandlingStatus.NotHandled)
                    whenNotHandled();
                return this;
            }
            
            if (!_source!.IsCompleted)
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