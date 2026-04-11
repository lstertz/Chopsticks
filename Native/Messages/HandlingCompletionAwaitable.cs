using Chopsticks.Messages.Exceptions;
using Chopsticks.Messages.Handlers.Sources;
using System;
using System.Runtime.CompilerServices;

namespace Chopsticks.Messages
{
    public struct HandlingCompletionAwaitable
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

            public HandlingCompletion GetResult() =>
                (HandlingCompletion)_source.GetResult().Status;

            public void OnCompleted(Action continuation) =>
                _source.OnCompleted(continuation);
        }


        private readonly Awaiter _awaiter;
        private readonly IHandlingPromiseSource _source;

        public HandlingCompletionAwaitable(IHandlingPromiseSource source)
        {
            _source = source;
            _awaiter = new(source);
        }

        public readonly Awaiter GetAwaiter() => _awaiter;


        public HandlingCompletionAwaitable ThrowIfNotHandled(
            string? customExceptionMessage = null)
        {
            if (!_awaiter.IsCompleted)
            {
                // If the source has not completed immediately, then it must be being handled.
                return this;
            }

            var completion = _awaiter.GetResult();
            if (completion == HandlingCompletion.NotHandled)
                throw new MessageNotHandledException(customExceptionMessage);

            return this;
        }
    }
}
