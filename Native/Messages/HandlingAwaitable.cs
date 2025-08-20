using Chopsticks.Messages.Handlers.Sources;
using System;
using System.Runtime.CompilerServices;

namespace Chopsticks.Messages
{
    public struct HandlingAwaitable
    {
        public readonly struct Awaiter : INotifyCompletion
        {
            public bool IsCompleted =>
                _source.IsCompleted;

            public bool ThrowIfFailed
            {
                get => _source.ThrowIfFailed;
                set => _source.ThrowIfFailed = value;
            }


            private readonly IHandlingPromiseSource _source;

            internal Awaiter(IHandlingPromiseSource source)
            {
                _source = source;
            }

            public HandlingResult GetResult() =>
                _source.GetResult();

            public void OnCompleted(Action continuation) =>
                _source.OnCompleted(continuation);
        }


        private readonly Awaiter _awaiter;
        private readonly IHandlingPromiseSource _source;

        public HandlingAwaitable(IHandlingPromiseSource source)
        {
            _source = source;
            _awaiter = new(source);
        }

        public readonly Awaiter GetAwaiter() => _awaiter;


        public HandlingAwaitable ThrowIfFailed()
        {
            if (!_awaiter.IsCompleted)
            {
                _awaiter.ThrowIfFailed = true;
                return this;
            }

            var result = _awaiter.GetResult();
            result.ThrowIfFailed();

            return this;
        }

        public HandlingAwaitable ThrowIfNotHandled(string? customExceptionMessage = null)
        {
            if (!_awaiter.IsCompleted)
            {
                // If the source has not completed immediately, then it must be being handled.
                return this;
            }

            var result = _awaiter.GetResult();
            result.ThrowIfNotHandled(customExceptionMessage);

            return this;
        }


        public HandlingAwaitable WhenNotHandled(Action whenNotHandled)
        {
            if (!_awaiter.IsCompleted)
            {
                // If the source has not completed immediately, then it must be being handled.

                return this;
            }

            var result = _awaiter.GetResult();
            if (result.Status == HandlingStatus.NotHandled)
                whenNotHandled();

            return this;
        }


        public readonly HandlingPromise ToPromise() => 
            new(_source);
    }
}