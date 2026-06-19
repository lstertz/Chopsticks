using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Chopsticks.Messages.Handlers.Sources.Pooling
{
    public interface IHandlingAwaiter
    {
        bool CanRelease { get; set; }

        bool IsCompleted { get; }

        public Action? OnCancelled { get; set; }

        public Action? OnCompletion { get; set; }

        public Action<HandlingResult>? OnCompletionWithResult { get; set; }

        public Action<IEnumerable<Exception>>? OnFailure { get; set; }

        public Action<HandlingResult>? OnNonSuccess { get; set; }

        public Action? OnSuccess { get; set; }

        public SynchronizationContext? FailureContext { get; set; }

        int Version { get; }


        void Release();

        HandlingResult GetResult();
    }

    public interface IHandlingAwaiter<TInnerAwaiter> : IHandlingAwaiter
    {
        void Init(TInnerAwaiter innerAwaiter, bool rethrowExceptions = false);
    }

    public abstract class BaseHandlingAwaiter<TInnerAwaiter> : 
        IHandlingAwaiter<TInnerAwaiter>
        where TInnerAwaiter : INotifyCompletion
    {
        public bool CanRelease
        {
            get => _canRelease;
            set
            {
                _canRelease = value;

                if (_canRelease && IsCompleted)
                    HandlingAwaiterPool.Instance.Return(this);
            }
        }
        private bool _canRelease;

        public abstract bool IsCompleted { get; }

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

        public int Version { get; private set; } = 0;


        protected TInnerAwaiter? InnerAwaiter { get; private set; }

        private HandlingResult _result = HandlingResult.Processing;

        private readonly Action _onCompleted;
        private bool _isInitialized;
        private bool _rethrowExceptions;

        protected BaseHandlingAwaiter()
        {
            _onCompleted = () =>
            {
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

        public void Init(TInnerAwaiter innerAwaiter, bool rethrowExceptions = false) 
        {
            InnerAwaiter = innerAwaiter;
            _rethrowExceptions = rethrowExceptions;
            _isInitialized = true;

            if (!IsCompleted)
                InnerAwaiter.OnCompleted(_onCompleted);
        }

        // TODO :: Add flags to prevent double returns.

        public virtual void Release()
        {
            Version++;
            _isInitialized = false;

            OnCancelled = null;
            OnCompletion = null;
            OnFailure = null;
            OnNonSuccess = null;
            OnSuccess = null;
            FailureContext = null;

            _result = HandlingResult.Processing;
            InnerAwaiter = default;
        }

        public HandlingResult GetResult()
        {
            if (!_isInitialized)
                throw new InvalidOperationException("The awaiter has not been initialized " +
                    "or has already been released.");

            HandlingResult result;

            if (_result.Status != HandlingStatus.Processing)
                return _result;  // Return cached result if already completed.

            if (!IsCompleted)
                return HandlingResult.Processing;

            try
            {
                GetInnerResult();
                result = HandlingResult.Success;
            }
            catch (OperationCanceledException)
            {
                result = HandlingResult.Cancelled;
                if (_rethrowExceptions)
                    throw;
            }
            catch (Exception ex)
            {
                result = HandlingResult.FromException(ex);
                if (_rethrowExceptions)
                    throw;
            }
            finally
            {
                if (_canRelease)
                    HandlingAwaiterPool.Instance.Return(this);
            }

            _result = result;
            return result;
        }

        protected abstract void GetInnerResult();



        protected void EnsureInitialized()
        {
            if (!_isInitialized)
                throw new InvalidOperationException("The awaiter has not been initialized " +
                    "or has already been released.");
        }
    }



    public class HandlingTaskAwaiter : BaseHandlingAwaiter<TaskAwaiter>
    {
        public override bool IsCompleted
        {
            get
            {
                EnsureInitialized();
                return InnerAwaiter.IsCompleted;
            }
        }


        protected override void GetInnerResult()
        {
            EnsureInitialized();
            InnerAwaiter.GetResult();
        }
    }



    public class HandlingAwaiterPool
    {
        public static readonly HandlingAwaiterPool Instance = new();  // TODO :: Re-think the singleton pattern here.

        private readonly ConcurrentDictionary<Type, ConcurrentBag<IHandlingAwaiter>> _available = new();

        public TAwaiter Rent<TAwaiter, TInnerAwaiter>(TInnerAwaiter innerAwaiter, 
            bool rethrowExceptions = false) 
            where TAwaiter : class, IHandlingAwaiter<TInnerAwaiter>, new()
            where TInnerAwaiter : INotifyCompletion
        {
            var type = typeof(TAwaiter);
            if (!_available.TryGetValue(type, out ConcurrentBag<IHandlingAwaiter>? bag))
            {
                bag = [];
                _available[type] = bag;
            }
    
            if (!bag.TryTake(out IHandlingAwaiter awaiter))
                awaiter = new TAwaiter();  // All available awaiters are already rented.
    
            TAwaiter tAwaiter = (TAwaiter)awaiter;
            tAwaiter.Init(innerAwaiter, rethrowExceptions);
            return (TAwaiter)awaiter;
        }

        public void Return<TAwaiter>(TAwaiter awaiter) 
            where TAwaiter : class, IHandlingAwaiter
        {
            var type = typeof(TAwaiter);
            if (!_available.TryGetValue(type, out ConcurrentBag<IHandlingAwaiter>? bag))
            {
                bag = [];
                _available[type] = bag;
            }

            awaiter.Release();
            bag.Add(awaiter);
        }
    }

    /// <summary>
    /// A pool of <see cref="IHandlingPromiseSource"/> objects.
    /// </summary>
    /// <typeparam name="TSource">The type of the source to pool.</typeparam>
    public class SourcePool<TSource> : ISourcePool 
        where TSource : class, IHandlingPromiseSource, new()
    {
        private readonly ConcurrentBag<TSource> _available =
            [
                new()
            ];


        /// <inheritdoc/>
        IHandlingPromiseSource ISourcePool.Rent() => Rent();

        /// <inheritdoc/>
        public TSource Rent()
        {
            if (!_available.TryTake(out TSource source))
                source = new();  // All available sources are already rented.

            return source;
        }


        /// <inheritdoc/>
        void ISourcePool.Return(IHandlingPromiseSource source)
        {
            if (source is TSource typedSource)
                Return(typedSource);
            else
                throw new ArgumentException($"The source being returned must be " +
                    $"of type {typeof(TSource).FullName}.", nameof(source));
        }

        /// <inheritdoc/>
        public void Return(TSource source)
        {
            source.Reset();
            _available.Add(source);
        }
    }
}