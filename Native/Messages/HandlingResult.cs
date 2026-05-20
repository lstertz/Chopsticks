using Chopsticks.Messages.Exceptions;
using System;
using System.Collections.Generic;

namespace Chopsticks.Messages
{
    /// <summary>
    /// The result of handling a message.
    /// </summary>
    public readonly struct HandlingResult
    {
        // TODO :: Possibly support a count of completed handlers.

        /// <summary>
        /// Builds a result indicating that the message was cancelled.
        /// </summary>
        public static HandlingResult Cancelled => new()
        {
            Status = HandlingStatus.Cancelled
        };

        /// <summary>
        /// Builds a result indicating that no handlers were found for the message.
        /// </summary>
        public static HandlingResult NoHandlers => new()
        {
            Status = HandlingStatus.NotHandled
        };

        /// <summary>
        /// Provides an in-progress result, that is, a result with a status 
        /// indicating that it is processing.
        /// </summary>
        public static HandlingResult Processing = new()
        {
            Status = HandlingStatus.Processing
        };

        /// <summary>
        /// Builds a result indicating that the message was handled successfully.
        /// </summary>
        public static HandlingResult Success => new()
        {
            Status = HandlingStatus.Success
        };

        /// <summary>
        /// Builds a result indicating that the message failed to be handled with an exception.
        /// </summary>
        /// <param name="exception">The exception that specifies the failure.</param>
        public static HandlingResult FromException(Exception exception) => new(exception);


        /// <summary>
        /// The exceptions that occurred during handling, if any.
        /// </summary>
        public IEnumerable<Exception> Exceptions
        {
            get
            {
                if (_exceptions is null)
                    yield break;

                if (_exceptions is Exception exception)
                    yield return exception;
                else if (_exceptions is Exception[] exceptions)
                    foreach (var ex in exceptions)
                        yield return ex;
            }
        }

        /// <summary>
        /// The status of the handling result.
        /// </summary>
        public HandlingStatus Status { get; private init; } = HandlingStatus.NotHandled;


        /// <summary>
        /// Captures either null (for success), a single exception (for unmerged failure), 
        /// or an array of exceptions (for merged failures).
        /// </summary>
        private readonly object? _exceptions;


        private HandlingResult(Exception exception)
        {
            Status = HandlingStatus.Failure;
            _exceptions = exception;
        }

        private HandlingResult(Exception[] exceptions)
        {
            Status = HandlingStatus.Failure;
            _exceptions = exceptions;
        }


        /// <summary>
        /// Merges this handling result with another handling result.
        /// </summary>
        /// <param name="other">The other handling result.</param>
        /// <returns>A handling result that is the logical combination of 
        /// both results.</returns>
        public HandlingResult MergeWith(HandlingResult other)
        {
            if (Status == HandlingStatus.NotHandled)
                return other;  // This isn't handled, return the other result.

            if (other.Status == HandlingStatus.NotHandled)
                return this;   // The other isn't handled, return this result.

            if (Status == HandlingStatus.Success)
                return other;  // Other is either success, failure, or cancelled.

            if (other.Status == HandlingStatus.Success)
                return this;   // This must be the only failure or cancelled.

            if (Status == HandlingStatus.Cancelled)
                return other;  // Other is either failure, which takes priority, or cancelled.

            if (other.Status == HandlingStatus.Cancelled)
                return this;  // This must be the only failure, which takes priority.

            // Both are failures, merge their exceptions.
            if (other._exceptions is Exception otherException)
            {
                if (_exceptions is Exception thisException)
                    return new HandlingResult([thisException, otherException]);
                else
                    return new HandlingResult(
                        MergeExceptions((Exception[])_exceptions!, otherException));
            }
            else
            {
                if (_exceptions is Exception thisException)
                    return new HandlingResult(
                        MergeExceptions((Exception[])other._exceptions!, thisException));
                else
                    return new HandlingResult(
                        MergeExceptions((Exception[])_exceptions!, (Exception[])other._exceptions!));
            }
        }

        private Exception[] MergeExceptions(Exception[] exceptions, Exception exception)
        {
            var merged = new Exception[exceptions.Length + 1];
            Array.Copy(exceptions, merged, exceptions.Length);
            merged[^1] = exception;

            return merged;
        }

        private Exception[] MergeExceptions(Exception[] exceptionsA, Exception[] exceptionsB)
        {
            var merged = new Exception[exceptionsA.Length + exceptionsB.Length];
            Array.Copy(exceptionsA, merged, exceptionsA.Length);
            Array.Copy(exceptionsB, 0, merged, exceptionsA.Length, exceptionsB.Length);

            return merged;
        }


        /// <summary>
        /// Executes the specified action regardless of the status.
        /// </summary>
        /// <param name="callback">The action to execute.</param>
        /// <returns>The current <see cref="HandlingResult"/> instance, 
        /// allowing for method chaining.</returns>
        public HandlingResult Always(Action<HandlingResult> callback)
        {
            callback(this);
            return this;
        }

        /// <summary>
        /// Throws an exception if the current status indicates a failure.
        /// </summary>
        /// <remarks>If the status is <see cref="HandlingStatus.Failure"/>, this method throws an
        /// exception. If a single exception is associated with the failure, it is thrown directly. 
        /// If multiple exceptions are associated, an <see cref="AggregateException"/> containing 
        /// all exceptions is thrown.
        /// </remarks>
        /// <exception cref="Exception">Thrown if a single exception is associated 
        /// with the failure.</exception>
        /// <exception cref="AggregateException">Thrown if multiple exceptions are associated 
        /// with the failure.</exception>
        public void ThrowIfFailed()
        {
            if (Status != HandlingStatus.Failure)
                return;

            if (_exceptions is Exception exception)
                throw exception;
            else if (_exceptions is Exception[] exceptions)
                throw new AggregateException(exceptions);
        }

        /// <summary>
        /// Throws an exception if the current status indicates that the message was 
        /// not handled.
        /// </summary>
        /// <param name="customExceptionMessage">A custom exception message.</param>
        /// <exception cref="MessageNotHandledException">The exception thrown 
        /// if the current status indicates that the message was not handled.</exception>
        public void ThrowIfNotHandled(string? customExceptionMessage = null)
        {
            if (Status != HandlingStatus.NotHandled)
                return;

            throw new MessageNotHandledException(customExceptionMessage);
        }

        /// <summary>
        /// Executes the specified action if the current handling result indicates a failure.
        /// </summary>
        /// <param name="whenFailed">The action to execute when the handling result has a 
        /// status of <see cref="HandlingStatus.Failure"/>.</param>
        /// <returns>The current <see cref="HandlingResult"/> instance, 
        /// allowing for method chaining.</returns>
        public HandlingResult WhenFailed(Action<IEnumerable<Exception>> whenFailed)
        {
            if (Status == HandlingStatus.Failure)
                whenFailed(Exceptions);
            return this;
        }

        /// <summary>
        /// Executes the specified action if the current handling result indicates that the 
        /// message was not handled.
        /// </summary>
        /// <param name="whenNotHandled">The action to execute when the handling status 
        /// is <see cref="HandlingStatus.NotHandled"/>.</param>
        /// <returns>The current <see cref="HandlingResult"/> instance, 
        /// allowing for method chaining.</returns>
        public HandlingResult WhenNotHandled(Action whenNotHandled)
        {
            if (Status == HandlingStatus.NotHandled)
                whenNotHandled();
            return this;
        }

        /// <summary>
        /// Executes the specified action if the handling result indicates success.
        /// </summary>
        /// <param name="whenSuccessful">The action to execute when the <see cref="Status"/> 
        /// is <see cref="HandlingStatus.Success"/>.</param>
        /// <returns>The current <see cref="HandlingResult"/> instance, 
        /// allowing for method chaining.</returns>
        public HandlingResult WhenSuccessful(Action whenSuccessful)
        {
            if (Status == HandlingStatus.Success)
                whenSuccessful();
            return this;
        }
    }
}
