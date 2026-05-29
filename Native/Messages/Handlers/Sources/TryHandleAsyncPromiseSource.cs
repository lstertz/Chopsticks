using System;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Chopsticks.Messages.Handlers.Sources
{
    public class TryHandleAsyncPromiseSource : 
        BaseHandlingPromiseSource<TaskAwaiter>
    {
        private static readonly ConcurrentBag<TryHandleAsyncPromiseSource> Pool = new();
        private static int _poolCount = 0;
        private const int MaxPoolSize = 64;
        
        /// <summary>
        /// Rents a TryHandleAsyncPromiseSource from the pool, or creates a new one if the pool is empty.
        /// </summary>
        public static TryHandleAsyncPromiseSource Rent()
        {
            if (Pool.TryTake(out var source))
            {
                Interlocked.Decrement(ref _poolCount);
                source.ResetForReuse();       // Reset state for fresh use
                source.ResetPoolReturnFlag(); // Must reset BEFORE returning to prevent race with stale Dispose()
                return source;
            }
            return new TryHandleAsyncPromiseSource();
        }
        
        /// <summary>
        /// Resets state for reuse from pool.
        /// </summary>
        private void ResetForReuse()
        {
            _isCompleted = false;
            _cachedResult = null;
        }
        
        /// <inheritdoc/>
        public override bool IsCompleted => 
            _isCompleted || _cachedResult.HasValue || InnerSource.IsCompleted;
        
        private bool _isCompleted;
        private HandlingResult? _cachedResult;

        /// <inheritdoc/>
        public override HandlingResult GetResult()
        {
            // Return cached result if already computed (supports multiple GetResult calls)
            if (_cachedResult.HasValue)
                return _cachedResult.Value;
            
            var result = GetResultWithoutReturn();
            _cachedResult = result;
            _isCompleted = true;
            ReturnToPoolIfCompleted();
            return result;
        }
        
        /// <inheritdoc/>
        protected override HandlingResult GetResultWithoutReturn()
        {
            // Return cached result if already computed
            if (_cachedResult.HasValue)
                return _cachedResult.Value;
            
            VerifyInitialized();

            try
            {
                InnerSource.GetResult();
                var result = HandlingResult.Success;
                _cachedResult = result;
                _isCompleted = true;
                return result;
            }
            catch (OperationCanceledException)
            {
                var result = HandlingResult.Cancelled;
                _cachedResult = result;
                _isCompleted = true;
                return result;
            }
            catch (Exception ex)
            {
                var result = HandlingResult.FromException(ex);
                _cachedResult = result;
                _isCompleted = true;
                return result;
            }
        }

        /// <inheritdoc/>
        public override void OnCompleted(Action continuation)
        {
            if (_isCompleted)
            {
                continuation();
                return;
            }
            VerifyInitialized();
            InnerSource.OnCompleted(continuation);
        }
        
        /// <inheritdoc/>
        protected override void ReturnToPoolIfCompleted()
        {
            // Guard against accessing IsCompleted after source has been disposed
            // (InnerSource would be default/null after disposal)
            if (TryMarkForPoolReturn())
            {
                ReturnToPool();
            }
        }
        
        private void ReturnToPool()
        {
            // NOTE: Don't clear _cachedResult here - it may still be needed 
            // for multiple GetResult() calls from the same consumer.
            // It will be cleared in Init() when the source is reused.
            base.Dispose();
            
            if (Interlocked.Increment(ref _poolCount) <= MaxPoolSize)
            {
                Pool.Add(this);
            }
            else
            {
                Interlocked.Decrement(ref _poolCount);
            }
        }
        
        /// <inheritdoc/>
        public override void Dispose()
        {
            if (TryMarkForPoolReturn())
            {
                ReturnToPool();
            }
        }
    }
}