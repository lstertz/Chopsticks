using System;

namespace Chopsticks.Messages
{
    /// <summary>
    /// The status of a message handling process.
    /// </summary>
    [Flags]
    public enum HandlingStatus
    {
        /// <summary>
        /// The status of a handling process that is still in progress.
        /// </summary>
        Processing = 0,

        /// <summary>
        /// The status indicating that no handlers were found for the message.
        /// </summary>
        /// <remarks>
        /// Under this status, no handling was performed, so it is considered 
        /// neither a success, a failure, nor a cancellation; it never started and 
        /// therefore never completed.
        /// </remarks>
        NotHandled = 1 << 0,


        /// <summary>
        /// The status indicating that the handling was successful.
        /// </summary>
        Success = 1 << 1,

        /// <summary>
        /// The status indicating that the handling failed with an exception.
        /// </summary>
        Failure = 1 << 2,

        /// <summary>
        /// The status indicating that the handling was cancelled before completion.
        /// </summary>
        Cancelled = 1 << 3,


        /// <summary>
        /// The status indicating that the handling was not successful, meaning that 
        /// it started, but was either cancelled or failed.
        /// </summary>
        NonSuccess = Failure | Cancelled,

        /// <summary>
        /// The status indicating that the handling was completed, meaning that 
        /// it started and ended, regardless of whether it was successful or not.
        /// </summary>
        Completed = Success | Failure | Cancelled,
    }
}
