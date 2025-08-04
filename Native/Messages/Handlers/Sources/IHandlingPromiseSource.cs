using System;
using System.Collections.Generic;

namespace Chopsticks.Messages.Handlers.Sources
{
    public interface IHandlingPromiseSource
    {
        public Action InitiateDefaultContinuations { get; }

        public Action? OnCancelled { get; set; }
        public Action<HandlingResult>? OnCompletion { get; set; }
        public Action<IEnumerable<Exception>>? OnFailure { get; set; }
        public Action<HandlingResult>? OnNonSuccess { get; set; }
        public Action? OnSuccess { get; set; }

        bool IsCompleted { get; }
        HandlingResult GetResult();
        void OnCompleted(Action continuation);
    }
}