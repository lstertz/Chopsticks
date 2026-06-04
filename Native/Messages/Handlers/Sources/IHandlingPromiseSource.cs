using System;
using System.Collections.Generic;
using System.Threading;

namespace Chopsticks.Messages.Handlers.Sources
{
    public interface IHandlingPromiseSource : IDisposable
    {
        Action InitiateDefaultContinuations { get; }

        Action? OnCancelled { get; set; }
        Action<HandlingResult>? OnCompletion { get; set; }
        Action<IEnumerable<Exception>>? OnFailure { get; set; }
        Action<HandlingResult>? OnNonSuccess { get; set; }
        Action? OnSuccess { get; set; }
        SynchronizationContext? FailureContext { get; set; }

        bool IsCompleted { get; }
        HandlingResult GetResult();
        void OnCompleted(Action continuation);

        /// <summary>
        /// Signals that this source is being consumed through a fluent
        /// <see cref="Chopsticks.Messages.HandlingResultPromise"/> /
        /// <see cref="Chopsticks.Messages.HandlingCompletionPromise"/> rather than a single-terminal
        /// <c>await</c>. The fluent API may register continuations after the source has completed,
        /// so the source must NOT return itself to a reuse pool (a recycled-then-re-rented instance
        /// would drop a late callback and corrupt the next dispatch). Pooled sources treat this as a
        /// one-way latch for the current generation; non-pooled sources may ignore it.
        /// </summary>
        void SuppressPooling();
    }
    
    /// <summary>
    /// Internal interface for poolable promise sources that support getting results
    /// without triggering auto-return to pool.
    /// </summary>
    internal interface IPoolablePromiseSource : IHandlingPromiseSource
    {
        /// <summary>
        /// Gets the result without triggering auto-return to pool.
        /// Used by wrapper sources to access inner source results.
        /// </summary>
        HandlingResult GetResultWithoutAutoReturn();
    }
}