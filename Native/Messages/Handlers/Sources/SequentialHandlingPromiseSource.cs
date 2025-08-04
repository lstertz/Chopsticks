using System;
using System.Threading;

namespace Chopsticks.Messages.Handlers.Sources
{
    public class SequentialHandlingPromiseSource<TMessage> :
        BaseHandlingPromiseSource<IMessageHandler<TMessage>[]>
    {
        /// <inheritdoc/>
        public override bool IsCompleted => _isCompleted;
        private bool _isCompleted = false;

        private CancellationToken _cancellationToken;
        private Action? _continuation;
        private HandlingAwaitable.Awaiter _currentAwaiter;
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
}