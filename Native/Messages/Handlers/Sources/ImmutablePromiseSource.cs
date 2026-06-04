using System;
using System.Collections.Generic;
using System.Threading;

namespace Chopsticks.Messages.Handlers.Sources
{
    /// <summary>
    /// An immutable promise source for pre-completed results.
    /// Used for static HandlingResultPromise instances (Success, NoHandlers)
    /// to avoid shared mutable state between callers.
    /// </summary>
    /// <remarks>
    /// All callback setters are no-ops since this source is already completed.
    /// Callbacks are invoked immediately inline when set (since IsCompleted is true,
    /// the HandlingResultPromise methods handle this case).
    /// </remarks>
    internal sealed class ImmutablePromiseSource : IHandlingPromiseSource
    {
        private readonly HandlingResult _result;

        public ImmutablePromiseSource(HandlingResult result) => _result = result;

        public bool IsCompleted => true;

        public HandlingResult GetResult() => _result;

        public void OnCompleted(Action continuation) => continuation();

        // Never pooled; nothing to suppress.
        public void SuppressPooling() { }

        public Action InitiateDefaultContinuations => static () => { };

        public Action? OnCancelled { get => null; set { } }
        public Action<HandlingResult>? OnCompletion { get => null; set { } }
        public Action<IEnumerable<Exception>>? OnFailure { get => null; set { } }
        public Action<HandlingResult>? OnNonSuccess { get => null; set { } }
        public Action? OnSuccess { get => null; set { } }
        public SynchronizationContext? FailureContext { get => null; set { } }

        public void Dispose() { }
    }
}
