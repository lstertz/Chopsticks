using Chopsticks.Messages.Handlers.Sources;
using System;
using System.Runtime.CompilerServices;

namespace Chopsticks.Messages
{
    public struct HandlingResultAwaitable
    {
        public readonly struct Awaiter : INotifyCompletion
        {
            public bool IsCompleted => _hasDirectResult || _source!.IsCompleted;

            private readonly IHandlingPromiseSource? _source;
            private readonly HandlingResult _directResult;
            private readonly bool _hasDirectResult;

            internal Awaiter(IHandlingPromiseSource source)
            {
                _source = source;
                _directResult = default;
                _hasDirectResult = false;
            }
            
            internal Awaiter(HandlingResult result)
            {
                _source = null;
                _directResult = result;
                _hasDirectResult = true;
            }

            public HandlingResult GetResult() => 
                _hasDirectResult ? _directResult : _source!.GetResult();

            public void OnCompleted(Action continuation)
            {
                if (_hasDirectResult)
                    continuation();
                else
                    _source!.OnCompleted(continuation);
            }
        }


        internal IHandlingPromiseSource? Source => _source;
        private readonly Awaiter _awaiter;
        private readonly IHandlingPromiseSource? _source;
        private readonly HandlingResult _directResult;
        private readonly bool _hasDirectResult;

        public HandlingResultAwaitable(IHandlingPromiseSource source)
        {
            _source = source;
            _directResult = default;
            _hasDirectResult = false;
            _awaiter = new(source);
        }
        
        /// <summary>
        /// Creates an awaitable from a direct result. Zero allocation for sync paths.
        /// </summary>
        public HandlingResultAwaitable(HandlingResult result)
        {
            _source = null;
            _directResult = result;
            _hasDirectResult = true;
            _awaiter = new(result);
        }

        public readonly Awaiter GetAwaiter() => _awaiter;


        public HandlingResultAwaitable ThrowIfNotHandled(string? customExceptionMessage = null)
        {
            if (_hasDirectResult)
            {
                _directResult.ThrowIfNotHandled(customExceptionMessage);
                return this;
            }
            
            if (!_awaiter.IsCompleted)
            {
                // If the source has not completed immediately, then it must be being handled.
                return this;
            }

            // Read WITHOUT auto-returning the source to its pool: a subsequent await on this same
            // awaitable performs the single terminal GetResult() that owns the recycle. Recycling
            // here would let the source be re-rented before that terminal read.
            var result = PeekResult();
            result.ThrowIfNotHandled(customExceptionMessage);

            return this;
        }


        public HandlingResultAwaitable WhenNotHandled(Action whenNotHandled)
        {
            if (_hasDirectResult)
            {
                if (_directResult.Status == HandlingStatus.NotHandled)
                    whenNotHandled();
                return this;
            }
            
            if (!_awaiter.IsCompleted)
            {
                // If the source has not completed immediately, then it must be being handled.
                return this;
            }

            var result = PeekResult();
            if (result.Status == HandlingStatus.NotHandled)
                whenNotHandled();

            return this;
        }

        /// <summary>
        /// Reads the completed result without triggering the source's auto-return to its pool.
        /// Only valid when the source has completed. Used by the non-terminal chainable inspectors
        /// so the single terminal await keeps ownership of the recycle.
        /// </summary>
        private readonly HandlingResult PeekResult() =>
            _source is IPoolablePromiseSource poolable
                ? poolable.GetResultWithoutAutoReturn()
                : _awaiter.GetResult();


        public readonly HandlingResultPromise ToPromise() => 
            _hasDirectResult ? new(_directResult) : new(_source!);
    }
}