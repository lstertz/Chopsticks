using System;
using System.Collections.Generic;
using System.Threading;
using Chopsticks.Messages.Handlers.Sources.Pooling;

namespace Chopsticks.Messages.Handlers.Sources
{
    /// <summary>
    /// Represents a promise source for handling messages.
    /// </summary>
    public interface IHandlingPromiseSource : IPooledSource
    {
        /// <summary>
        /// Gets the action to initiate default continuations.
        /// </summary>
        Action InitiateDefaultContinuations { get; }

        /// <summary>
        /// Gets or sets the action to invoke when the promise is cancelled.
        /// </summary>
        Action? OnCancelled { get; set; }

        /// <summary>
        /// Gets or sets the action to invoke when the promise is completed.
        /// </summary>
        Action<HandlingResult>? OnCompletion { get; set; }

        /// <summary>
        /// Gets or sets the action to invoke when the promise fails.
        /// </summary>
        Action<IEnumerable<Exception>>? OnFailure { get; set; }

        /// <summary>
        /// Gets or sets the action to invoke when the promise is not successful.
        /// </summary>
        Action<HandlingResult>? OnNonSuccess { get; set; }

        /// <summary>
        /// Gets or sets the action to invoke when the promise is successful.
        /// </summary>
        Action? OnSuccess { get; set; }

        /// <summary>
        /// Gets or sets the synchronization context for failure.
        /// </summary>
        SynchronizationContext? FailureContext { get; set; }


        /// <summary>
        /// Gets a value indicating whether the promise is completed.
        /// </summary>
        bool IsCompleted { get; }

        /// <summary>
        /// Gets the result of the promise.
        /// </summary>
        HandlingResult GetResult();

        /// <summary>
        /// Registers a continuation to be invoked when the promise is completed.
        /// </summary>
        void OnCompleted(Action continuation);
    }
}