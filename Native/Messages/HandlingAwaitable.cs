using System;

namespace Chopsticks.Messages
{
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
}