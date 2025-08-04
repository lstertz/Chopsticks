using System;
using System.Collections.Generic;

namespace Chopsticks.Messages.Handlers.Sources
{
    public interface IHandlingPromiseSource
    {
        Action InitiateDefaultContinuations { get; }

        Action? OnCancelled { get; set; }
        Action<HandlingResult>? OnCompletion { get; set; }
        Action<IEnumerable<Exception>>? OnFailure { get; set; }
        Action<HandlingResult>? OnNonSuccess { get; set; }
        Action? OnSuccess { get; set; }
        bool ThrowIfFailed { get; set; }

        bool IsCompleted { get; }
        HandlingResult GetResult();
        void OnCompleted(Action continuation);
    }
}