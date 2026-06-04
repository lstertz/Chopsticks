using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Chopsticks.Messages.Registration.Handlers;

namespace Chopsticks.Messages.Handlers.Sources
{
    public class SequentialHandlingPromiseSource<TMessage, TContext> :
        BaseHandlingPromiseSource<BaseRegisteredHandler<TMessage, TContext>[]>
        where TContext : IMessageContext<TMessage>, new()
    {
        // Pooling is re-enabled, but ONLY for the single-terminal await path. The earlier crash/
        // drop came from the fluent consumption path (TryHandleAsync(...).ToPromise().OnCompletion):
        // a completed source could be recycled (via GetResult inside InitiateDefaultContinuations on
        // the handler thread) WHILE the consumer was still chaining .OnCompletion(), so the consumer
        // registered onto a re-rented instance and the callback was dropped. The source's lifetime
        // there is not owned by a single consumer.
        //
        // Ownership model that makes reuse safe:
        //  - await path: the awaiter performs exactly ONE terminal GetResult(); that is the single,
        //    well-defined recycle point. No callbacks are registered after completion. -> pooled.
        //  - fluent path: HandlingResultPromise/HandlingCompletionPromise call SuppressPooling() on
        //    the source BEFORE registering any completion continuation, so this instance is never
        //    returned to the pool (it is simply disposed and GC-reclaimed, exactly as before). Late
        //    fluent registrations then hit a live, non-recycled instance and fire correctly.
        // The generation counter (ResetPoolReturnFlag / TryMarkForPoolReturn) still rejects stale
        // Dispose() calls. FallbackConcurrencyTests covers both paths under heavy concurrency.
        private static readonly ConcurrentBag<SequentialHandlingPromiseSource<TMessage, TContext>> Pool = new();
        private static int _poolCount = 0;
        private const int MaxPoolSize = 64;

        public static SequentialHandlingPromiseSource<TMessage, TContext> Rent()
        {
            if (Pool.TryTake(out var source))
            {
                Interlocked.Decrement(ref _poolCount);
                source.ResetForReuse();
                source.ResetPoolReturnFlag(); // Increment generation BEFORE handing out (rejects stale Dispose).
                return source;
            }
            return new SequentialHandlingPromiseSource<TMessage, TContext>();
        }

        private void ResetForReuse()
        {
            _isCompleted = false;
            _continuation = null;
            _currentAwaiter = default;
            _currentIndex = 0;
            _context = default!;
            _aggregateStatus = HandlingStatus.NotHandled;
            _accumulatedExceptions = null;
            _cachedResult = null;
        }

        // Guards publication of _isCompleted / _continuation so a continuation registered
        // concurrently with completion is never dropped (which would hang the awaiter).
        private readonly object _gate = new();
        
        /// <inheritdoc/>
        public override bool IsCompleted => _isCompleted;

        // Volatile so that a thread observing _isCompleted == true is guaranteed to also observe
        // the _cachedResult write that happens-before it in Step (release/acquire ordering). Without
        // this, a consumer could read _isCompleted == true but a stale (NotHandled) result, causing
        // Completed-only callbacks (e.g. OnCompletion) to be silently skipped.
        private volatile bool _isCompleted = false;

        private Action? _continuation;
        private HandlingResultAwaitable.Awaiter _currentAwaiter;
        private int _currentIndex = 0;
        private TContext _context;
        private HandlingStatus _aggregateStatus;
        private List<Exception>? _accumulatedExceptions;
        
        /// <summary>
        /// Cached result to preserve state after source is returned to pool.
        /// This allows HandlingResultPromise to still read the result.
        /// </summary>
        private HandlingResult? _cachedResult;

        private readonly Action _onHandlerCompletion;


        public SequentialHandlingPromiseSource() : base() =>
            _onHandlerCompletion = OnHandlerCompletion;
        
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
            base.Dispose();

            // Fluent consumers (promises) suppress pooling: they may still register callbacks on
            // this instance after completion, so it must stay uniquely theirs and be GC-reclaimed
            // rather than re-rented. Only the single-terminal await path returns to the pool.
            if (PoolingSuppressed)
                return;

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


        public void Run(TContext context)
        {
            VerifyInitialized();

            _aggregateStatus = HandlingStatus.NotHandled;
            _context = context;

            Step();
        }
        
        /// <summary>
        /// Continues execution from a specific handler index with pre-accumulated state.
        /// Used when sync fast-path encounters an async handler.
        /// </summary>
        /// <param name="context">The message context.</param>
        /// <param name="startIndex">The index of the first async handler to run.</param>
        /// <param name="currentStatus">Already-accumulated status from sync handlers.</param>
        /// <param name="exceptions">Already-accumulated exceptions (may be null).</param>
        /// <param name="pendingAwaiter">The awaiter for the first async handler.</param>
        public void RunFromIndex(
            TContext context, 
            int startIndex, 
            HandlingStatus currentStatus,
            List<Exception>? exceptions,
            HandlingResultAwaitable.Awaiter pendingAwaiter)
        {
            VerifyInitialized();
            
            _context = context;
            _currentIndex = startIndex;
            _aggregateStatus = currentStatus;
            
            // Transfer accumulated exceptions
            if (exceptions is { Count: > 0 })
            {
                _accumulatedExceptions ??= new List<Exception>(exceptions.Count + 4);
                _accumulatedExceptions.AddRange(exceptions);
            }
            
            // Resume with the pending async awaiter
            _currentAwaiter = pendingAwaiter;
            _currentAwaiter.OnCompleted(_onHandlerCompletion);
        }


        /// <inheritdoc/>
        public override HandlingResult GetResult()
        {
            // Return cached result if available (even after pool return)
            if (_cachedResult.HasValue)
                return _cachedResult.Value;

            // SAFETY: GetResult is contractually expected only after IsCompleted. If a caller
            // reads early (incomplete), we must NOT cache or recycle: handler continuations are
            // still pending and would otherwise fire into a source that has been reset/re-rented,
            // dereferencing a default awaiter (NRE / host crash). Return a best-effort result and
            // leave the source live so its own completion can run to term.
            if (!_isCompleted)
                return GetResultWithoutReturn();

            var result = GetResultWithoutReturn();
            _cachedResult = result;
            ReturnToPoolIfCompleted();
            return result;
        }
        
        /// <inheritdoc/>
        protected override HandlingResult GetResultWithoutReturn()
        {
            // Return cached result if available
            if (_cachedResult.HasValue)
                return _cachedResult.Value;
            
            VerifyInitialized();
            return BuildFinalResult();
        }

        /// <inheritdoc/>
        public override void OnCompleted(Action continuation)
        {
            VerifyInitialized();

            bool completed;
            lock (_gate)
            {
                completed = _isCompleted;
                if (!completed)
                    _continuation = continuation;
            }

            // Invoke outside the lock to avoid reentrant lock acquisition and to keep the
            // continuation (which may dispatch again) off the gate.
            if (completed)
                continuation();
        }


        private void OnHandlerCompletion()
        {
            var handlerResult = _currentAwaiter.GetResult();
            MergeStatus(handlerResult);

            _currentIndex++;
            Step();
        }
        
        private void MergeStatus(HandlingResult result)
        {
            if (result.Status == HandlingStatus.NotHandled)
                return;
            
            if (result.Status == HandlingStatus.Failure)
            {
                _accumulatedExceptions ??= new List<Exception>(4);
                result.AddExceptionsTo(_accumulatedExceptions);
                _aggregateStatus = HandlingStatus.Failure;
                return;
            }
            
            if (_aggregateStatus == HandlingStatus.Failure)
                return;
            
            if (result.Status == HandlingStatus.Cancelled)
            {
                if (_aggregateStatus != HandlingStatus.Failure)
                    _aggregateStatus = HandlingStatus.Cancelled;
                return;
            }
            
            if (_aggregateStatus == HandlingStatus.NotHandled || 
                _aggregateStatus == HandlingStatus.Success)
            {
                _aggregateStatus = result.Status;
            }
        }
        
        private HandlingResult BuildFinalResult()
        {
            if (_accumulatedExceptions is { Count: > 0 })
                return HandlingResult.FromExceptions(_accumulatedExceptions);
            
            return _aggregateStatus switch
            {
                HandlingStatus.Success => HandlingResult.Success,
                HandlingStatus.Cancelled => HandlingResult.Cancelled,
                _ => HandlingResult.NoHandlers
            };
        }

        private void Step()
        {
            if (_currentIndex == InnerSource!.Length)
            {
                // Cache result before marking complete and invoking callbacks.
                _cachedResult = BuildFinalResult();

                // Publish completion and capture the continuation under the gate so a continuation
                // registered concurrently by OnCompleted is never dropped (avoids an awaiter hang).
                Action? continuation;
                lock (_gate)
                {
                    _isCompleted = true;
                    continuation = _continuation;
                }
                continuation?.Invoke();

                return;
            }

            var registeredHandler = InnerSource[_currentIndex];
            _currentAwaiter = registeredHandler.HandleAsync(_context).GetAwaiter();

            if (_currentAwaiter.IsCompleted)
                OnHandlerCompletion();                              // Synchronous handler.
            else
                _currentAwaiter.OnCompleted(_onHandlerCompletion);  // Asynchronous handler.
        }
    }
}