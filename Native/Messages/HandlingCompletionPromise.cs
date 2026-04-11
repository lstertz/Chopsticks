using Chopsticks.Messages.Exceptions;
using Chopsticks.Messages.Handlers.Sources;
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
    public struct HandlingCompletionPromise
    {
        public HandlingCompletion Completion => _source.IsCompleted ?
            (HandlingCompletion)_source.GetResult().Status : 
            HandlingCompletion.NotHandled;

        internal IHandlingPromiseSource Source => _source;
        private readonly IHandlingPromiseSource _source;
        private readonly SynchronizationContext? _asyncContext;


        public HandlingCompletionPromise(IHandlingPromiseSource source,
            SynchronizationContext? asyncContext = null)
        {
            _source = source;
            _asyncContext = asyncContext;

            if (!_source.IsCompleted)
                _source.OnCompleted(_source.InitiateDefaultContinuations);
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
        public HandlingCompletionPromise ThrowIfNotHandled(
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

        public HandlingCompletionPromise WhenCompleted(Action whenCompleted)
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

        public HandlingCompletionPromise WhenNotHandled(Action whenNotHandled)
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
