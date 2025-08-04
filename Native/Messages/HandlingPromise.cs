using Chopsticks.Messages.Abstractions;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Chopsticks.Messages
{
    public interface IHandlingPromiseSource
    {
        public Action InitiateDefaultContinuations { get; }

        public Action? OnCancelled { get; set; }
        public Action<HandlingResult>? OnCompletion { get; set; }
        public Action<IEnumerable<Exception>>? OnFailure { get; set; }
        public Action<HandlingResult>? OnNonSuccess { get; set; }
        public Action? OnSuccess { get; set; }

        bool IsCompleted { get; }
        HandlingResult GetResult();
        void OnCompleted(Action continuation);
    }

    public struct HandlingPromise
    {
        public static HandlingPromise NoHandlers => new(_noHandlersSource);
        private static readonly IHandlingPromiseSource _noHandlersSource = 
            new SyncHandlingPromiseSource().Init(HandlingResult.NoHandlers);

        public static HandlingPromise Success => new(_successSource);
        private static readonly IHandlingPromiseSource _successSource =
            new SyncHandlingPromiseSource().Init(HandlingResult.Success);


        public HandlingStatus Status => _source.IsCompleted ? 
            _source.GetResult().Status : HandlingStatus.Processing;

        private readonly IHandlingPromiseSource _source;


        internal HandlingPromise(IHandlingPromiseSource source)
        {
            _source = source;
            if (!_source.IsCompleted)
                _source.OnCompleted(_source.InitiateDefaultContinuations);
        }

        public HandlingPromise OnCancelled(Action onCancelled)
        {
            if (!_source.IsCompleted)
            {
                _source.OnCancelled = onCancelled;
                return this;
            }

            var result = _source.GetResult();
            if (result.Status == HandlingStatus.Cancelled)
                onCancelled();

            return this;
        }

        public HandlingPromise OnCompletion(Action<HandlingResult> onCompletion)
        {
            if (!_source.IsCompleted)
            {
                _source.OnCompletion = onCompletion;
                return this;
            }

            var result = _source.GetResult();
            if ((result.Status | HandlingStatus.Completed) != 0)
                onCompletion(result);

            return this;
        }

        public HandlingPromise OnFailure(Action<IEnumerable<Exception>> onFailure)
        {
            if (!_source.IsCompleted)
            {
                _source.OnFailure = onFailure;
                return this;
            }

            var result = _source.GetResult();
            if (result.Status == HandlingStatus.Failure)
                onFailure(result.Exceptions);

            return this;
        }

        public HandlingPromise OnNonSuccess(Action<HandlingResult> onNonSuccess)
        {
            if (!_source.IsCompleted)
            {
                _source.OnNonSuccess = onNonSuccess;
                return this;
            }

            var result = _source.GetResult();
            if ((result.Status | HandlingStatus.NonSuccess) != 0)
                onNonSuccess(result);

            return this;
        }

        public HandlingPromise OnSuccess(Action onSuccess)
        {
            if (!_source.IsCompleted)
            {
                _source.OnSuccess = onSuccess;
                return this;
            }

            var result = _source.GetResult();
            if (result.Status == HandlingStatus.Success)
                onSuccess();

            return this;
        }

        public HandlingPromise WhenNotHandled(Action whenNotHandled)
        {
            if (!_source.IsCompleted)
            {
                // If the source has not completed immediately, then it must be being handled.
                return this;
            }

            var result = _source.GetResult();
            if (result.Status == HandlingStatus.NotHandled)
                whenNotHandled();

            return this;
        }
    }

    public struct HandlingAwaitable
    {
        private readonly HandlingAwaiter _awaiter;

        internal HandlingAwaitable(IHandlingPromiseSource source)
        {
            _awaiter = new(source);
        }

        public readonly HandlingAwaiter GetAwaiter() => _awaiter;


        public HandlingAwaitable WhenNotHandled(Action whenNotHandled)
        {
            if (_awaiter.IsCompleted)
            {
                var result = _awaiter.GetResult();
                if (result.Status == HandlingStatus.NotHandled)
                    whenNotHandled();
            }

            // If the source has not completed immediately, then it must be being handled.
            // So we do not set the action.

            return this;
        }
    }

    public readonly struct HandlingAwaiter : INotifyCompletion
    {
        public bool IsCompleted =>
            _source.IsCompleted;

        private readonly IHandlingPromiseSource _source;

        internal HandlingAwaiter(IHandlingPromiseSource source)
        {
            _source = source;
        }

        public HandlingResult GetResult() =>
            _source.GetResult();

        public void OnCompleted(Action continuation) =>
            _source.OnCompleted(continuation);
    }


    // TODO :: Implement pooling for all handling promise source implementations.
    public abstract class BaseHandlingPromiseSource<TInnerSource> : IHandlingPromiseSource
    {
        public abstract bool IsCompleted { get; }

        public Action InitiateDefaultContinuations { get; private set; }

        public Action? OnCancelled { get; set; }
        public Action<HandlingResult>? OnCompletion { get; set; }
        public Action<IEnumerable<Exception>>? OnFailure { get; set; }
        public Action<HandlingResult>? OnNonSuccess { get; set; }
        public Action? OnSuccess { get; set; }


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
                else if ((result.Status & HandlingStatus.Completed) != 0)
                    OnCompletion?.Invoke(result);
                else if (result.Status == HandlingStatus.Failure)
                    OnFailure?.Invoke(result.Exceptions);
                else if ((result.Status & HandlingStatus.NonSuccess) != 0)
                    OnNonSuccess?.Invoke(result);
                else if (result.Status == HandlingStatus.Success)
                    OnSuccess?.Invoke();
            };
        }

        public IHandlingPromiseSource Init(TInnerSource innerSource)
        {
            _isInitialized = true;
            InnerSource = innerSource;

            return this;
        }

        public virtual void Dispose()
        {
            InnerSource = default;

            _isInitialized = false;

            OnCancelled = null;
            OnCompletion = null;
            OnFailure = null;
            OnNonSuccess = null;
            OnSuccess = null;
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

    // TODO :: Update Multicast handler and registrar.


    public class SequentialHandlingPromiseSource<TMessage> :
        BaseHandlingPromiseSource<IMessageHandler<TMessage>[]>
    {
        /// <inheritdoc/>
        public override bool IsCompleted => _isCompleted;
        private bool _isCompleted = false;

        private CancellationToken _cancellationToken;
        private Action? _continuation;
        private HandlingAwaiter _currentAwaiter;
        private int _currentIndex = 0;
        private TMessage _message;
        private HandlingResult _result;

        private readonly Action _onHandlerCompletion;


        internal SequentialHandlingPromiseSource() : base() => 
            _onHandlerCompletion = OnHandlerCompletion;

        /// <inheritdoc/>
        public override void Dispose()
        {
            base.Dispose();

            _isCompleted = false;

            _continuation = null;
            _currentAwaiter = default;
            _currentIndex = -1;
        }


        public void Run(TMessage message, CancellationToken cancellationToken)
        {
            VerifyInitialized();

            _result = HandlingResult.NoHandlers;
            _cancellationToken = cancellationToken;
            _message = message;

            Step();
        }


        /// <inheritdoc/>
        public override HandlingResult GetResult()
        {
            VerifyInitialized();

            return _result;
        }

        /// <inheritdoc/>
        public override void OnCompleted(Action continuation)
        {
            VerifyInitialized();

            if (_isCompleted)
                continuation();
            else
                _continuation = continuation;
        }


        private void OnHandlerCompletion()
        {
            _result = _result.MergeWith(_currentAwaiter.GetResult());

            _currentIndex++;
            Step();
        }

        private void Step()
        {
            if (_currentIndex == InnerSource!.Length)
            {
                _isCompleted = true;
                _continuation?.Invoke();

                return;
            }

            var handler = InnerSource[_currentIndex];
            _currentAwaiter = handler.HandleAsync(_message, _cancellationToken).GetAwaiter();

            if (_currentAwaiter.IsCompleted)
                OnHandlerCompletion();                              // Synchronous handler.
            else
                _currentAwaiter.OnCompleted(_onHandlerCompletion);  // Asynchronous handler.
        }
    }

    public class SyncHandlingPromiseSource :
        BaseHandlingPromiseSource<HandlingResult>
    {
        /// <inheritdoc/>
        public override bool IsCompleted => true;


        /// <inheritdoc/>
        public override HandlingResult GetResult() => InnerSource;

        /// <inheritdoc/>
        public override void OnCompleted(Action continuation) => 
            continuation();
    }

    public class TaskHandlingPromiseSource : 
        BaseHandlingPromiseSource<TaskAwaiter>
    {
        /// <inheritdoc/>
        public override bool IsCompleted => 
            InnerSource.IsCompleted;


        /// <inheritdoc/>
        public override HandlingResult GetResult()
        {
            VerifyInitialized();

            try
            {
                InnerSource.GetResult();
                return HandlingResult.Success;
            }
            catch (OperationCanceledException)
            {
                return HandlingResult.Cancelled;
            }
            catch (Exception ex)
            {
                return HandlingResult.FromException(ex);
            }
        }

        /// <inheritdoc/>
        public override void OnCompleted(Action continuation)
        {
            VerifyInitialized();
            InnerSource.OnCompleted(continuation);
        }
    }
}