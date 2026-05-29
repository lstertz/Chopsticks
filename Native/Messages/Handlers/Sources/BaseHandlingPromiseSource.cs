using System;
using System.Collections.Generic;
using System.Threading;

namespace Chopsticks.Messages.Handlers.Sources
{
    public abstract class BaseHandlingPromiseSource<TInnerSource> : IPoolablePromiseSource
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
        
        /// <summary>
        /// Generation counter that increments on each Rent(). Used to detect stale
        /// Dispose() calls from previous owners. Thread-safe via Interlocked.
        /// </summary>
        private int _generation = 0;
        
        /// <summary>
        /// The generation at which this source was last returned to pool.
        /// If _returnedGeneration == _generation, the source has been returned.
        /// </summary>
        private int _returnedGeneration = -1;
        
        /// <summary>
        /// Static cached callback for SynchronizationContext.Post to avoid closure allocation.
        /// </summary>
        private static readonly SendOrPostCallback PostThrowIfFailedCallback = 
            state => ((HandlingResult)state!).ThrowIfFailed();


        protected BaseHandlingPromiseSource()
        {
            InitiateDefaultContinuations = () =>
            {
                // If already returned to pool (via a direct GetResult() call),
                // the callbacks have been cleared and there's nothing to do.
                if (!_isInitialized)
                    return;
                
                // Get result WITHOUT auto-return - we need the callbacks first
                var result = GetResultWithoutReturn();

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

                // Use static callback with boxed state to avoid closure allocation
                // Boxing still allocates, but avoids the more expensive closure + delegate allocation
                FailureContext?.Post(PostThrowIfFailedCallback, result);
                
                // Now return to pool after all callbacks are done
                ReturnToPoolIfCompleted();
            };
        }

        public IHandlingPromiseSource Init(TInnerSource innerSource)
        {
            // NOTE: Do NOT reset _returnedToPool here - it must be reset in Rent()
            // BEFORE the source is handed to the caller. Resetting here creates a
            // race condition where a stale Dispose() from the previous owner can
            // corrupt the new owner's state.
            _isInitialized = true;
            InnerSource = innerSource;

            return this;
        }
        
        /// <summary>
        /// Increments the generation counter. Must be called in Rent() before returning
        /// the source to a new caller. This ensures stale Dispose() calls from previous
        /// owners will fail because they'll be checking against an old generation.
        /// </summary>
        protected void ResetPoolReturnFlag()
        {
            // Increment generation - any stale Dispose() calls will now fail
            // because they'll try to mark generation N as returned when we're now on N+1
            Interlocked.Increment(ref _generation);
        }

        public virtual void Dispose()
        {
            InnerSource = default;

            _isInitialized = false;
            // NOTE: Do NOT reset _returnedToPool here - keep it at 1 so that
            // stale references calling Dispose() after auto-return will fail
            // TryMarkForPoolReturn() and become no-ops.

            OnCancelled = null;
            OnCompletion = null;
            OnFailure = null;
            OnNonSuccess = null;
            OnSuccess = null;
            FailureContext = null;
        }
        
        /// <summary>
        /// Attempts to return this source to its pool. Returns true if successfully marked for return.
        /// Thread-safe - uses generation tracking to detect stale Dispose() calls from previous owners.
        /// </summary>
        protected bool TryMarkForPoolReturn()
        {
            // Read current generation - this is the generation we're trying to return
            int currentGen = Volatile.Read(ref _generation);
            
            // Spin-loop CAS to atomically update _returnedGeneration
            while (true)
            {
                int prevReturned = Volatile.Read(ref _returnedGeneration);
                
                // If already returned this generation, fail
                if (prevReturned == currentGen)
                    return false;
                
                // Try to atomically set _returnedGeneration to currentGen
                if (Interlocked.CompareExchange(ref _returnedGeneration, currentGen, prevReturned) == prevReturned)
                    return true;
                
                // CAS failed, retry
            }
        }
        
        /// <summary>
        /// Called by derived classes to return to pool after result is consumed.
        /// Override in derived classes that implement pooling.
        /// </summary>
        protected virtual void ReturnToPoolIfCompleted()
        {
            // Default implementation does nothing.
            // Derived classes with pooling override this.
        }


        public abstract HandlingResult GetResult();
        
        /// <summary>
        /// Gets the result without triggering auto-return to pool.
        /// Used internally by InitiateDefaultContinuations to invoke callbacks first.
        /// </summary>
        protected abstract HandlingResult GetResultWithoutReturn();
        
        /// <summary>
        /// IPoolablePromiseSource implementation - exposes GetResultWithoutReturn for wrapper sources.
        /// Returns NotHandled if the source has already been disposed.
        /// </summary>
        HandlingResult IPoolablePromiseSource.GetResultWithoutAutoReturn()
        {
            // If already disposed, return a safe default
            if (!_isInitialized)
                return HandlingResult.NoHandlers;
            return GetResultWithoutReturn();
        }

        public abstract void OnCompleted(Action continuation);


        protected void VerifyInitialized()
        {
            if (!_isInitialized)
                throw new InvalidOperationException(
                    "The promise source either has not been initialized or has been disposed.");
        }
    }
}