using System;
using System.Collections.Generic;

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



        public HandlingPromise OnCancellation(Action onCancellation)
        {
            if ((_currentResult.Status & HandlingStatus.Cancelled) == 0)
                onCancellation();

            // TODO :: Otherwise, cache the action to be called when the status is 
            //             a cancellation later.
            return this;
        }

        public HandlingPromise OnCompletion(Action<HandlingPromise> onCompletion)
        {
            if ((_currentResult.Status & HandlingStatus.Completed) == 0)
                onCompletion(this);
            // TODO :: Otherwise, cache the action to be called when the status is 
            //             a completion later.
            return this;

        }

        public HandlingPromise OnFailure(Action<IEnumerable<Exception>> onFailure)
        {
            if ((_currentResult.Status & HandlingStatus.Failure) == 0)
                onFailure(_currentResult.Exceptions);

            // TODO :: Otherwise, cache the action to be called when the status is 
            //             a failure later.
            return this;
        }

        public HandlingPromise OnNonSuccess(Action<HandlingPromise> onNonSuccess)
        {
            if ((_currentResult.Status & HandlingStatus.NonSuccess) == 0)
                onNonSuccess(this);

            // TODO :: Otherwise, cache the action to be called when the status is 
            //             a non-success later.
            return this;
        }

        public HandlingPromise OnSuccess(Action onSuccess)
        {
            if ((_currentResult.Status & HandlingStatus.Success) == 0)
                onSuccess();

            // TODO :: Otherwise, cache the action to be called when the status is 
            //             a success later.

            return this;
        }

        public HandlingPromise WhenNotHandled(Action whenNotHandled)
        {
            if ((_currentResult.Status & HandlingStatus.NotHandled) == 0)
                whenNotHandled();
            return this;
        }
    }
}
