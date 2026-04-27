using System;
using System.Runtime.CompilerServices;
using Chopsticks.Messages.Exceptions;
using Chopsticks.Messages.Handlers.Sources;

namespace Chopsticks.Messages
{
    /// <summary>
    /// The awaitable result of a message handling operation that has been 
    /// dispatched through a non-try handler.
    /// </summary>
    /// <remarks>
    /// This is a reduced subset of <see cref="HandlingResultAwaitable"/> that only 
    /// represents the observable outcomes after failure and cancellation 
    /// have been thrown as exceptions.
    /// </remarks>
    public readonly struct HandlingCompletionAwaitable
    {
        /// <summary>
        /// The awaiter for the <see cref="HandlingCompletionAwaitable"/>.
        /// </summary>
        public readonly struct Awaiter : INotifyCompletion
        {
            /// <summary>
            /// Whether the awaiter has completed.
            /// </summary>
            public bool IsCompleted =>
                _source.IsCompleted;


            private readonly IHandlingPromiseSource _source;

            internal Awaiter(IHandlingPromiseSource source)
            {
                _source = source;
            }

            public HandlingCompletion GetResult() =>
                (HandlingCompletion)_source.GetResult().Status;

            public void OnCompleted(Action continuation) =>
                _source.OnCompleted(continuation);
        }


        private readonly Awaiter _awaiter;
        private readonly IHandlingPromiseSource _source;

        public HandlingCompletionAwaitable(IHandlingPromiseSource source)
        {
            _source = source;
            _awaiter = new(source);
        }


        /// <summary>
        /// Gets the awaiter for the <see cref="HandlingCompletionAwaitable"/>.
        /// </summary>
        /// <returns>
        /// The awaiter for the <see cref="HandlingCompletionAwaitable"/>.
        /// </returns>
        public readonly Awaiter GetAwaiter() => _awaiter;


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
        public readonly HandlingCompletionAwaitable ThrowIfNotHandled(
            string? customExceptionMessage = null)
        {
            if (!_awaiter.IsCompleted)
            {
                // If the source has not completed immediately, then it must be being handled.
                return this;
            }

            var completion = _awaiter.GetResult();
            if (completion == HandlingCompletion.NotHandled)
                throw new MessageNotHandledException(customExceptionMessage);

            return this;
        }
    }
}
