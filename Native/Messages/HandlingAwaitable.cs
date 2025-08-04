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

        internal HandlingAwaitable(IHandlingPromiseSource source)
        {
            _awaiter = new(source);
        }

        public readonly Awaiter GetAwaiter() => _awaiter;


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
}