using System;
using System.Collections.Generic;
using System.Threading;
using Chopsticks.Messages.Handlers.Sources.Pooling;

namespace Chopsticks.Messages.Handlers.Sources
{
    /// <summary>
    /// Base class for handling promise sources.
    /// </summary>
    /// <typeparam name="TInnerSource">The type of the inner source.</typeparam>
    public abstract class BaseHandlingPromiseSource<TInnerSource> :
        IHandlingPromiseSource
    {
        /// <inheritdoc/>
        public abstract bool IsCompleted { get; }

        /// <inheritdoc/>
        public Action InitiateDefaultContinuations { get; private set; }

        /// <inheritdoc/>
        public Action? OnCancelled { get; set; }

        /// <inheritdoc/>
        public Action? OnCompletion { get; set; }

        /// <inheritdoc/>
        public Action<HandlingResult>? OnCompletionWithResult { get; set; }

        /// <inheritdoc/>
        public Action<IEnumerable<Exception>>? OnFailure { get; set; }

        /// <inheritdoc/>
        public Action<HandlingResult>? OnNonSuccess { get; set; }

        /// <inheritdoc/>
        public Action? OnSuccess { get; set; }

        /// <inheritdoc/>
        public SynchronizationContext? FailureContext { get; set; }

        /// <inheritdoc/>
        public int Version { get; private set; } = 0;


        /// <summary>
        /// The inner source that the promise source wraps.
        /// </summary>
        protected TInnerSource? InnerSource { get; private set; }

        private bool _isInitialized = false;


        /// <summary>
        /// Creates a new instance of the 
        /// <see cref="BaseHandlingPromiseSource{TInnerSource}"/> class.
        /// </summary>
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
                {
                    OnCompletion?.Invoke();
                    OnCompletionWithResult?.Invoke(result);
                }

                try
                {
                    FailureContext?.Post(_ => result.ThrowIfFailed(), null);
                }
                catch
                {
                    // If Post throws synchronously, run ThrowIfFailed on this thread so failures are observed.
                    result.ThrowIfFailed();
                }
            };
        }

        /// <summary>
        /// Initializes the promise source with the given inner source.
        /// </summary>
        /// <param name="innerSource">The inner source to initialize the promise source with.</param>
        /// <returns>
        /// The initialized promise source.
        /// </returns>
        public IHandlingPromiseSource Init(TInnerSource innerSource)
        {
            _isInitialized = true;
            InnerSource = innerSource;

            return this;
        }

        /// <inheritdoc cref="IPooledSource.Reset"/>
        public virtual void Reset()
        {
            Version++;

            InnerSource = default;

            _isInitialized = false;

            OnCancelled = null;
            OnCompletion = null;
            OnFailure = null;
            OnNonSuccess = null;
            OnSuccess = null;
            FailureContext = null;
        }

        /// <inheritdoc cref="IHandlingPromiseSource.GetResult()"/>
        public abstract HandlingResult GetResult();

        /// <inheritdoc cref="IHandlingPromiseSource.OnCompleted(Action)"/>
        public abstract void OnCompleted(Action continuation);


        /// <summary>
        /// Verifies that the promise source has been initialized.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// The promise source has not been initialized.
        /// </exception>
        protected void VerifyInitialized()
        {
            if (!_isInitialized)
                throw new InvalidOperationException(
                    "The promise source either has not been initialized or has been disposed.");
        }
    }
}