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