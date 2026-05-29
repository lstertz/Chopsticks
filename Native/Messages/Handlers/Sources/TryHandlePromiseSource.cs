using System;
using System.Collections.Concurrent;
using System.Threading;

namespace Chopsticks.Messages.Handlers.Sources
{
    public class TryHandlePromiseSource :
        BaseHandlingPromiseSource<HandlingResult>
    {
        private static readonly ConcurrentBag<TryHandlePromiseSource> Pool = new();
        private static int _poolCount = 0;
        private const int MaxPoolSize = 64;
        
        /// <summary>
        /// Rents a TryHandlePromiseSource from the pool, or creates a new one if the pool is empty.
        /// </summary>
        public static TryHandlePromiseSource Rent()
        {
            if (Pool.TryTake(out var source))
            {
                Interlocked.Decrement(ref _poolCount);
                source._cachedResult = null;  // Clear stale cached result from previous use
                source.ResetPoolReturnFlag(); // Must reset BEFORE returning to prevent race with stale Dispose()
                return source;
            }
            return new TryHandlePromiseSource();
        }
        
        /// <inheritdoc/>
        public override bool IsCompleted => true;
        
        private HandlingResult? _cachedResult;

        /// <inheritdoc/>
        public override HandlingResult GetResult()
        {
            // NOTE: Reading the result does NOT release the source to the pool. Returning here
            // (in addition to an explicit Dispose) created a double-return: if a concurrent
            // thread re-rented this instance between GetResult() and Dispose(), the stale
            // Dispose would observe the advanced generation, pass the "already returned" guard,
            // and return the in-use instance — corrupting the new owner. Release is owned solely
            // by Dispose() (or the completion lifecycle), giving exactly-once return semantics.
            if (_cachedResult.HasValue)
                return _cachedResult.Value;

            var result = InnerSource;
            _cachedResult = result;
            return result;
        }
        
        /// <inheritdoc/>
        protected override HandlingResult GetResultWithoutReturn()
        {
            if (_cachedResult.HasValue)
                return _cachedResult.Value;
            return InnerSource;
        }

        /// <inheritdoc/>
        public override void OnCompleted(Action continuation) => 
            continuation();
        
        /// <inheritdoc/>
        protected override void ReturnToPoolIfCompleted()
        {
            // TryMarkForPoolReturn ensures we only return once
            if (TryMarkForPoolReturn())
            {
                ReturnToPool();
            }
        }
        
        private void ReturnToPool()
        {
            // Clear state before returning to pool
            base.Dispose();
            
            if (Interlocked.Increment(ref _poolCount) <= MaxPoolSize)
            {
                Pool.Add(this);
            }
            else
            {
                // Pool is full, don't add
                Interlocked.Decrement(ref _poolCount);
            }
        }
        
        /// <inheritdoc/>
        public override void Dispose()
        {
            // Manual dispose also returns to pool if not already returned
            if (TryMarkForPoolReturn())
            {
                ReturnToPool();
            }
        }
    }
}