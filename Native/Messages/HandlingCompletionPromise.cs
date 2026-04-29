using System;
using System.Threading;
using Chopsticks.Messages.Exceptions;
using Chopsticks.Messages.Handlers.Sources;

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
    public readonly struct HandlingCompletionPromise
    {
        /// <summary>
        /// Gets the completion status of the message handling operation.
        /// </summary>
        public HandlingCompletion Completion => _source.IsCompleted ?
            (HandlingCompletion)_source.GetResult().Status :
            HandlingCompletion.NotHandled;

        internal readonly IHandlingPromiseSource Source => _source;
        private readonly IHandlingPromiseSource _source;


        public HandlingCompletionPromise(IHandlingPromiseSource source,
            SynchronizationContext? asyncContext = null)
        {
            _source = source;
            _source.FailureContext = asyncContext;

            if (!_source.IsCompleted)
                _source.OnCompleted(_source.InitiateDefaultContinuations);
            else
            {
                _source.InitiateDefaultContinuations();

                // Try to throw directly if this was synchronous.
                _source.GetResult().ThrowIfFailed();
            }
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
        /// The current <see cref="HandlingCompletionPromise"/> instance, 
        /// allowing for method chaining.
        /// </returns>
        public readonly HandlingCompletionPromise ThrowIfNotHandled(
            string? customExceptionMessage = null)
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

        /// <summary>
        /// The continuation performed when the promise has completed, 
        /// meaning that the message has been handled by at least one handler.
        /// </summary>
        /// <param name="whenCompleted">The action to perform when the promise has completed.</param>
        /// <returns>
        /// The current <see cref="HandlingCompletionPromise"/> instance, 
        /// allowing for method chaining.
        /// </returns>
        public readonly HandlingCompletionPromise WhenCompleted(Action whenCompleted)
        {
            if (!_source.IsCompleted)
            {
                _source.OnCompletion = (_) => whenCompleted();
                return this;
            }

            var result = _source.GetResult();
            if ((result.Status & HandlingStatus.Completed) != 0)
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
        /// <returns>
        /// The current <see cref="HandlingCompletionPromise"/> instance, 
        /// allowing for method chaining.
        /// </returns>
        public readonly HandlingCompletionPromise WhenNotHandled(Action whenNotHandled)
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
