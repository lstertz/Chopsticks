using System;
using System.Collections.Generic;
using System.Threading;

namespace Chopsticks.Messages.Handlers.Sources
{
    // TODO :: Implement pooling for all handling promise source implementations.
    public abstract class BaseHandlingPromiseSource<TInnerSource> : IHandlingPromiseSource
    {
        public abstract bool IsCompleted { get; }

        public Action InitiateDefaultContinuations { get; private set; }

        public Action? OnCancelled { get; set; }
        public Action<HandlingResult>? OnCompletion { get; set; }
        public Action<IEnumerable<Exception>>? OnFailure { get; set; }
        public Action<HandlingResult>? OnNonSuccess { get; set; }
        public Action? OnSuccess { get; set; }
        public SynchronizationContext? FailureContext { get; set; }


        protected TInnerSource? InnerSource { get; private set; }
        private bool _isInitialized = false;


        protected BaseHandlingPromiseSource()
        {
            InitiateDefaultContinuations = () =>
            {
                VerifyInitialized();
                var result = GetResult();

                if (result.Status == HandlingStatus.Cancelled)
                    OnCancelled?.Invoke();
                else if (result.Status == HandlingStatus.Failure)
                    OnFailure?.Invoke(result.Exceptions);
                else if (result.Status == HandlingStatus.Success)
                    OnSuccess?.Invoke();
                
                if ((result.Status & HandlingStatus.NonSuccess) != 0)
                    OnNonSuccess?.Invoke(result);
                if ((result.Status & HandlingStatus.Completed) != 0)
                    OnCompletion?.Invoke(result);

                FailureContext?.Post(_ => result.ThrowIfFailed(), null);
            };
        }

        public IHandlingPromiseSource Init(TInnerSource innerSource)
        {
            _isInitialized = true;
            InnerSource = innerSource;

            return this;
        }

        public virtual void Dispose()
        {
            InnerSource = default;

            _isInitialized = false;

            OnCancelled = null;
            OnCompletion = null;
            OnFailure = null;
            OnNonSuccess = null;
            OnSuccess = null;
            FailureContext = null;
        }


        public abstract HandlingResult GetResult();

        public abstract void OnCompleted(Action continuation);


        protected void VerifyInitialized()
        {
            if (!_isInitialized)
                throw new InvalidOperationException(
                    "The promise source either has not been initialized or has been disposed.");
        }
    }
}