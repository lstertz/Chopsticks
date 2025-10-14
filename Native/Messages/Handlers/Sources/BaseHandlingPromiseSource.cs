using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Chopsticks.Messages.Handlers.Sources
{
    public interface ISourcePool
    {
        IHandlingPromiseSource Rent();

        void Return(IHandlingPromiseSource source);
    }

    // TODO :: Determine where the pool instance is held.
    //        Possibly a static instance on each source implementation.
    //        Needs to be accessible to the awaiters that hold the source reference.

    public class SourcePool<TSource>
        where TSource : class, IHandlingPromiseSource, new()
    {
        private readonly ConcurrentBag<TSource> _available =
            [
                new(),
                new(),
                new(),
                new()
            ];
        private readonly ConcurrentDictionary<TSource, TSource> _rented = new();

        public IHandlingPromiseSource Rent()
        {
            if (!_available.TryTake(out TSource source))
                source = new();  // All available sources are already rented.

            _rented.TryAdd(source, source);
            return source;
        }

        public void Return(IHandlingPromiseSource source)
        {
            if (!_rented.TryRemove((TSource)source, out var rentedSource))
                return;  // This source wasn't rented.

            rentedSource.Reset();
            _available.Add(rentedSource);
        }
    }

    /// <summary>
    /// Defines the required functionality for a source to be pooled.
    /// </summary>
    public interface IPooledSource
    {
        /// <summary>
        /// Resets the pooled source so it can be safely used again.
        /// </summary>
        void Reset();
    }

    // TODO :: Implement pooling for all handling promise source implementations.
    public abstract class BaseHandlingPromiseSource<TInnerSource> :
        IHandlingPromiseSource
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

        /// <inheritdoc cref="IPooledSource.Reset"/>
        public virtual void Reset()
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