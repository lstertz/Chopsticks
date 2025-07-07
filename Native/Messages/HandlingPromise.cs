using System;

namespace Chopsticks.Messages
{
    public struct HandlingPromise
    {
        public static HandlingPromise NoHandlers { get; } = new(HandlingResult.NoHandlers);

        public static HandlingPromise Success { get; } = new(HandlingResult.Success);


        private HandlingResult _currentResult;


        public HandlingPromise(HandlingResult initialResult)
        {
            _currentResult = initialResult;
        }

        public HandlingPromise OnSuccess(Action<HandlingResult> onSuccess)
        {
            if (_currentResult.Status == HandlingStatus.Success)
            {
                onSuccess(_currentResult);
            }

            // TODO :: Otherwise, cache the action to be called when the status is 
            //             a success later.

            return this;
        }

        public HandlingPromise OnUnprocessed(Action<HandlingResult> onUnprocessed)
        {
            if (_currentResult.Status == HandlingStatus.Unprocessed)
            {
                onUnprocessed(_currentResult);
            }

            return this;
        }
    }
}
