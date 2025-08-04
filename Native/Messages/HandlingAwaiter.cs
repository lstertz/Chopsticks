using System;
using System.Runtime.CompilerServices;

namespace Chopsticks.Messages
{
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
}