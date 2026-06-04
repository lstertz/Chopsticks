using System;
using System.Collections.Generic;
using System.Threading;

namespace Chopsticks.Messages.Handlers.Sources
{
    public abstract class BaseHandlingPromiseSource<TInnerSource> : IPoolablePromiseSource
    {
        public abstract bool IsCompleted { get; }

        public Action InitiateDefaultContinuations { get; private set; }

        // Completion-callback registration is synchronized against completion so a callback
        // registered concurrently with (or after) completion is invoked exactly once, never
        // dropped. Without this, the fluent HandlingResultPromise.OnXxx methods raced the
        // async completion path and could silently lose a callback (hanging the awaiter).
        private readonly object _continuationLock = new();
        private bool _continuationsFired;
        private HandlingResult _firedResult;

        private Action? _onCancelled;
        private Action<HandlingResult>? _onCompletion;
        private Action<IEnumerable<Exception>>? _onFailure;
        private Action<HandlingResult>? _onNonSuccess;
        private Action? _onSuccess;
        private SynchronizationContext? _failureContext;

        public Action? OnCancelled
        {
            get => _onCancelled;
            set
            {
                bool fired;
                HandlingResult result;
                lock (_continuationLock)
                {
                    fired = _continuationsFired;
                    result = _firedResult;
                    if (!fired) _onCancelled = value;
                }
                if (fired && value is not null && result.Status == HandlingStatus.Cancelled)
                    value();
            }
        }

        public Action<HandlingResult>? OnCompletion
        {
            get => _onCompletion;
            set
            {
                bool fired;
                HandlingResult result;
                lock (_continuationLock)
                {
                    fired = _continuationsFired;
                    result = _firedResult;
                    if (!fired) _onCompletion = value;
                }
                if (fired && value is not null && (result.Status & HandlingStatus.Completed) != 0)
                    value(result);
            }
        }

        public Action<IEnumerable<Exception>>? OnFailure
        {
            get => _onFailure;
            set
            {
                bool fired;
                HandlingResult result;
                lock (_continuationLock)
                {
                    fired = _continuationsFired;
                    result = _firedResult;
                    if (!fired) _onFailure = value;
                }
                if (fired && value is not null && result.Status == HandlingStatus.Failure)
                    value(result.Exceptions);
            }
        }

        public Action<HandlingResult>? OnNonSuccess
        {
            get => _onNonSuccess;
            set
            {
                bool fired;
                HandlingResult result;
                lock (_continuationLock)
                {
                    fired = _continuationsFired;
                    result = _firedResult;
                    if (!fired) _onNonSuccess = value;
                }
                if (fired && value is not null && (result.Status & HandlingStatus.NonSuccess) != 0)
                    value(result);
            }
        }

        public Action? OnSuccess
        {
            get => _onSuccess;
            set
            {
                bool fired;
                HandlingResult result;
                lock (_continuationLock)
                {
                    fired = _continuationsFired;
                    result = _firedResult;
                    if (!fired) _onSuccess = value;
                }
                if (fired && value is not null && result.Status == HandlingStatus.Success)
                    value();
            }
        }

        public SynchronizationContext? FailureContext
        {
            get => _failureContext;
            set
            {
                bool fired;
                HandlingResult result;
                lock (_continuationLock)
                {
                    fired = _continuationsFired;
                    result = _firedResult;
                    if (!fired) _failureContext = value;
                }
                if (fired && value is not null)
                    value.Post(PostThrowIfFailedCallback, result);
            }
        }


        protected TInnerSource? InnerSource { get; private set; }
        private bool _isInitialized = false;

        /// <summary>
        /// When set, the source must not return itself to a reuse pool for the current generation.
        /// Set via <see cref="SuppressPooling"/> when the source is handed to a fluent promise that
        /// may register continuations after completion. Reset per generation in <see cref="Init"/>.
        /// Pooled derived sources consult <see cref="PoolingSuppressed"/>; others ignore it.
        /// </summary>
        private volatile bool _poolingSuppressed;

        /// <inheritdoc/>
        public void SuppressPooling() => _poolingSuppressed = true;

        /// <summary>
        /// Whether pool-return has been suppressed for the current generation (see
        /// <see cref="SuppressPooling"/>). Derived pooled sources must not recycle when this is true.
        /// </summary>
        protected bool PoolingSuppressed => _poolingSuppressed;
        
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

                // Publish completion and snapshot all registered callbacks atomically. Any
                // callback registered after this point observes _continuationsFired and invokes
                // itself inline (in its setter), so none can be dropped by a registration race.
                Action? onCancelled;
                Action<IEnumerable<Exception>>? onFailure;
                Action? onSuccess;
                Action<HandlingResult>? onNonSuccess;
                Action<HandlingResult>? onCompletion;
                SynchronizationContext? failureContext;
                lock (_continuationLock)
                {
                    _firedResult = result;
                    _continuationsFired = true;
                    onCancelled = _onCancelled;
                    onFailure = _onFailure;
                    onSuccess = _onSuccess;
                    onNonSuccess = _onNonSuccess;
                    onCompletion = _onCompletion;
                    failureContext = _failureContext;
                }

                if (result.Status == HandlingStatus.Cancelled)
                    onCancelled?.Invoke();
                else if (result.Status == HandlingStatus.Failure)
                    onFailure?.Invoke(result.Exceptions);
                else if (result.Status == HandlingStatus.Success)
                    onSuccess?.Invoke();
                
                if ((result.Status & HandlingStatus.NonSuccess) != 0)
                    onNonSuccess?.Invoke(result);
                if ((result.Status & HandlingStatus.Completed) != 0)
                    onCompletion?.Invoke(result);

                // Use static callback with boxed state to avoid closure allocation
                // Boxing still allocates, but avoids the more expensive closure + delegate allocation
                failureContext?.Post(PostThrowIfFailedCallback, result);
                
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
            lock (_continuationLock)
            {
                _continuationsFired = false;
                _firedResult = default;
            }
            // New generation defaults to poolable; a fluent promise re-suppresses after wrapping.
            _poolingSuppressed = false;
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

            // Clear via backing fields (the synchronized setters intentionally refuse to mutate
            // once completion has fired). Do NOT reset _continuationsFired/_firedResult here:
            // Dispose runs as part of completion (ReturnToPoolIfCompleted), so clearing the
            // "fired" flag mid-lifecycle would let a late OnXxx registration store a callback that
            // never fires. The fired state is reset only in Init(), i.e. when reused for a new run.
            lock (_continuationLock)
            {
                _onCancelled = null;
                _onCompletion = null;
                _onFailure = null;
                _onNonSuccess = null;
                _onSuccess = null;
                _failureContext = null;
            }
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