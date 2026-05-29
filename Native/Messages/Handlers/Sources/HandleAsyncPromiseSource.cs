using System;

namespace Chopsticks.Messages.Handlers.Sources
{
    /// <summary>
    /// A wrapper promise source used by HandleAsync methods that throws on failure.
    /// </summary>
    /// <remarks>
    /// NOTE: This source is NOT pooled because HandlingCompletionAwaitable structs may
    /// hold references to it long after completion. Pooling would cause race conditions
    /// where a reused source could corrupt the state observed by old awaitable structs.
    /// The inner sources (TryHandleAsyncPromiseSource etc.) are still pooled.
    /// </remarks>
    public class HandleAsyncPromiseSource :
        BaseHandlingPromiseSource<IHandlingPromiseSource>
    {
        // Cached state to preserve after completion
        private bool _isCompleted;
        private HandlingResult? _cachedResult;
        
        /// <summary>
        /// Creates a new HandleAsyncPromiseSource.
        /// </summary>
        public static HandleAsyncPromiseSource Rent() => new HandleAsyncPromiseSource();
        
        /// <inheritdoc/>
        public override bool IsCompleted =>
            _isCompleted || (InnerSource?.IsCompleted ?? true);

        /// <inheritdoc/>
        public override HandlingResult GetResult()
        {
            // Return cached result if available
            if (_cachedResult.HasValue)
            {
                var cached = _cachedResult.Value;
                if (cached.Status == HandlingStatus.Failure)
                    throw new AggregateException(cached.Exceptions);
                if (cached.Status == HandlingStatus.Cancelled)
                    throw new OperationCanceledException();
                return cached;
            }
            
            var result = GetResultWithoutReturn();
            _cachedResult = result;
            _isCompleted = true;
            
            // Dispose inner source but NOT this wrapper
            InnerSource?.Dispose();

            if (result.Status == HandlingStatus.Failure)
                throw new AggregateException(result.Exceptions);

            if (result.Status == HandlingStatus.Cancelled)
                throw new OperationCanceledException();

            return result;
        }
        
        /// <inheritdoc/>
        protected override HandlingResult GetResultWithoutReturn()
        {
            // Return cached result if available
            if (_cachedResult.HasValue)
                return _cachedResult.Value;
            
            // Guard against disposed inner source
            if (InnerSource == null)
            {
                _isCompleted = true;
                _cachedResult = HandlingResult.NoHandlers;
                return HandlingResult.NoHandlers;
            }
            
            // For wrapper sources, access inner result without triggering its auto-return
            HandlingResult result;
            if (InnerSource is IPoolablePromiseSource poolableSource)
                result = poolableSource.GetResultWithoutAutoReturn();
            else
                result = InnerSource.GetResult();
            
            // Cache for subsequent access
            _cachedResult = result;
            _isCompleted = true;
            return result;
        }

        /// <inheritdoc/>
        public override void OnCompleted(Action continuation)
        {
            // If already completed, invoke immediately
            if (_isCompleted || _cachedResult.HasValue)
            {
                continuation();
                return;
            }
            
            if (InnerSource != null)
                InnerSource.OnCompleted(continuation);
            else
                continuation(); // No source, consider it completed
        }
        
        /// <inheritdoc/>
        public override void Dispose()
        {
            // Cache result before disposing inner source
            if (InnerSource != null && !_cachedResult.HasValue && InnerSource.IsCompleted)
            {
                try
                {
                    if (InnerSource is IPoolablePromiseSource poolable)
                        _cachedResult = poolable.GetResultWithoutAutoReturn();
                    else
                        _cachedResult = InnerSource.GetResult();
                    _isCompleted = true;
                }
                catch
                {
                    _isCompleted = true;
                    _cachedResult = HandlingResult.NoHandlers;
                }
            }
            
            // Dispose inner source (returns it to its pool)
            InnerSource?.Dispose();
            base.Dispose();
        }
    }
}