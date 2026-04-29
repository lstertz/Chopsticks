using System;
using Chopsticks.Messages.Registration.Handlers;

namespace Chopsticks.Messages.Handlers.Sources
{
    public class SequentialHandlingPromiseSource<TMessage, TContext> :
        BaseHandlingPromiseSource<BaseRegisteredHandler<TMessage, TContext>[]>
        where TContext : IMessageContext<TMessage>, new()
    {
        /// <inheritdoc/>
        public override bool IsCompleted => _isCompleted;
        private bool _isCompleted = false;

        private Action? _continuation;
        private HandlingResultAwaitable.Awaiter _currentAwaiter;
        private int _currentIndex = 0;
        private TContext _context;
        private HandlingResult _result;

        private readonly Action _onHandlerCompletion;


        public SequentialHandlingPromiseSource() : base() =>
            _onHandlerCompletion = OnHandlerCompletion;

        /// <inheritdoc/>
        public override void Reset()
        {
            base.Reset();

            _isCompleted = false;

            _continuation = null;
            _currentAwaiter = default;
            _currentIndex = 0;
        }


        public void Run(TContext context)
        {
            VerifyInitialized();

            _result = HandlingResult.NoHandlers;
            _context = context;

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

            var registeredHandler = InnerSource[_currentIndex];
            _currentAwaiter = registeredHandler.HandleAsync(_context).GetAwaiter();

            if (_currentAwaiter.IsCompleted)
                OnHandlerCompletion();                              // Synchronous handler.
            else
                _currentAwaiter.OnCompleted(_onHandlerCompletion);  // Asynchronous handler.
        }
    }
}