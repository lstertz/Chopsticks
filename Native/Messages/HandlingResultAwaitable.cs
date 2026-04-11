using Chopsticks.Messages.Handlers.Sources;
using System;
using System.Runtime.CompilerServices;

namespace Chopsticks.Messages
{
    public struct HandlingResultAwaitable
    {
        public readonly struct Awaiter : INotifyCompletion
        {
            public bool IsCompleted =>
                _source.IsCompleted;


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


        internal IHandlingPromiseSource Source => _source;
        private readonly Awaiter _awaiter;
        private readonly IHandlingPromiseSource _source;

        public HandlingResultAwaitable(IHandlingPromiseSource source)
        {
            _source = source;
            _awaiter = new(source);
        }

        public readonly Awaiter GetAwaiter() => _awaiter;


        public HandlingResultAwaitable ThrowIfNotHandled(string? customExceptionMessage = null)
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


        public HandlingResultAwaitable WhenNotHandled(Action whenNotHandled)
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


        public readonly HandlingResultPromise ToPromise() => 
            new(_source);
    }
}