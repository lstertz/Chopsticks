using System;
using System.Collections.Generic;
using Chopsticks.Messages.Registration.Handlers;

namespace Chopsticks.Messages.Handlers.Sources
{
    public class SequentialHandlingPromiseSource<TMessage, TContext> :
        BaseHandlingPromiseSource<BaseRegisteredHandler<TMessage, TContext>[]>
        where TContext : IMessageContext<TMessage>, new()
    {
        // POOLING DISABLED (intentional): This source registers completion callbacks on
        // external async primitives and is consumed via struct awaiters/promises that may be
        // copied and read AFTER the source completes. Recycling the instance allowed a recycled
        // source to be reset/re-rented while an old consumer still read it, producing torn reads
        // of the multi-field _currentAwaiter struct (null _source with _hasDirectResult == false)
        // and intermittent NullReferenceExceptions in OnHandlerCompletion that crashed the host.
        // Safely re-enabling reuse requires a result-ownership redesign (copy the result out to the
        // consumer and forbid reads of a recycled source). Until then, each dispatch gets a fresh
        // instance: the zero-allocation sync fast path never reaches this source, and async
        // multicast already allocates Task state machines, so the extra small object is negligible
        // next to correctness/crash-freedom.
        public static SequentialHandlingPromiseSource<TMessage, TContext> Rent() =>
            new SequentialHandlingPromiseSource<TMessage, TContext>();

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
            // Reuse is disabled (see Rent). Release base resources and let GC reclaim the
            // instance; never reset and re-pool, which is what enabled the use-after-recycle race.
            base.Dispose();
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