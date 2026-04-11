using System;

namespace Chopsticks.Messages
{
    /// <summary>
    /// The completion status of a message handling process that has been 
    /// dispatched through a non-try handler.
    /// </summary>
    /// <remarks>
    /// This is a reduced subset of <see cref="HandlingStatus"/> that only 
    /// represents the observable outcomes after failure and cancellation 
    /// have been thrown as exceptions.
    /// </remarks>
    [Flags]
    public enum HandlingCompletion
    {
        /// <summary>
        /// Indicates that no handlers were found for the message.
        /// </summary>
        /// <remarks>
        /// This is a non-handling status. No handling was performed, so it is considered 
        /// neither a success, a failure, nor a cancellation; it never started and 
        /// therefore never completed any handling.
        /// </remarks>
        NotHandled = 0,

        /// <summary>
        /// The completion indicating that handling was successfully performed.
        /// </summary>
        Successful = 1 << 1,
    }
}
