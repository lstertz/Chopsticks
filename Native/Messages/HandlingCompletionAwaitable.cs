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
            public bool IsCompleted => _hasDirectResult || _source!.IsCompleted;

            private readonly IHandlingPromiseSource? _source;
            private readonly HandlingResult _directResult;
            private readonly bool _hasDirectResult;

            internal Awaiter(IHandlingPromiseSource source)
            {
                _source = source;
                _directResult = default;
                _hasDirectResult = false;
            }
            
            internal Awaiter(HandlingResult result)
            {
                _source = null;
                _directResult = result;
                _hasDirectResult = true;
            }

            public HandlingCompletion GetResult() =>
                _hasDirectResult ? (HandlingCompletion)_directResult.Status : (HandlingCompletion)_source!.GetResult().Status;

            public void OnCompleted(Action continuation)
            {
                if (_hasDirectResult)
                    continuation();
                else
                    _source!.OnCompleted(continuation);
            }
        }


        private readonly Awaiter _awaiter;
        private readonly IHandlingPromiseSource? _source;
        private readonly HandlingResult _directResult;
        private readonly bool _hasDirectResult;

        public HandlingCompletionAwaitable(IHandlingPromiseSource source)
        {
            _source = source;
            _directResult = default;
            _hasDirectResult = false;
            _awaiter = new(source);
        }
        
        /// <summary>
        /// Creates an awaitable from a direct result. Zero allocation for sync paths.
        /// </summary>
        public HandlingCompletionAwaitable(HandlingResult result)
        {
            _source = null;
            _directResult = result;
            _hasDirectResult = true;
            _awaiter = new(result);
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
            if (_hasDirectResult)
            {
                if ((HandlingCompletion)_directResult.Status == HandlingCompletion.NotHandled)
                    throw new MessageNotHandledException(customExceptionMessage);
                return this;
            }
            
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
