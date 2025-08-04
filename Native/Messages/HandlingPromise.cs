using System;
using System.Collections.Generic;

namespace Chopsticks.Messages
{
    public struct HandlingPromise
    {
        public static HandlingPromise NoHandlers => new(_noHandlersSource);
        private static readonly IHandlingPromiseSource _noHandlersSource = 
            new SyncHandlingPromiseSource().Init(HandlingResult.NoHandlers);

        public static HandlingPromise Success => new(_successSource);
        private static readonly IHandlingPromiseSource _successSource =
            new SyncHandlingPromiseSource().Init(HandlingResult.Success);


        public HandlingStatus Status => _source.IsCompleted ? 
            _source.GetResult().Status : HandlingStatus.Processing;

        private readonly IHandlingPromiseSource _source;


        internal HandlingPromise(IHandlingPromiseSource source)
        {
            _source = source;
            if (!_source.IsCompleted)
                _source.OnCompleted(_source.InitiateDefaultContinuations);
        }

        public HandlingPromise OnCancelled(Action onCancelled)
        {
            if (!_source.IsCompleted)
            {
                _source.OnCancelled = onCancelled;
                return this;
            }

            var result = _source.GetResult();
            if (result.Status == HandlingStatus.Cancelled)
                onCancelled();

            return this;
        }

        public HandlingPromise OnCompletion(Action<HandlingResult> onCompletion)
        {
            if (!_source.IsCompleted)
            {
                _source.OnCompletion = onCompletion;
                return this;
            }

            var result = _source.GetResult();
            if ((result.Status | HandlingStatus.Completed) != 0)
                onCompletion(result);

            return this;
        }

        public HandlingPromise OnFailure(Action<IEnumerable<Exception>> onFailure)
        {
            if (!_source.IsCompleted)
            {
                _source.OnFailure = onFailure;
                return this;
            }

            var result = _source.GetResult();
            if (result.Status == HandlingStatus.Failure)
                onFailure(result.Exceptions);

            return this;
        }

        public HandlingPromise OnNonSuccess(Action<HandlingResult> onNonSuccess)
        {
            if (!_source.IsCompleted)
            {
                _source.OnNonSuccess = onNonSuccess;
                return this;
            }

            var result = _source.GetResult();
            if ((result.Status | HandlingStatus.NonSuccess) != 0)
                onNonSuccess(result);

            return this;
        }

        public HandlingPromise OnSuccess(Action onSuccess)
        {
            if (!_source.IsCompleted)
            {
                _source.OnSuccess = onSuccess;
                return this;
            }

            var result = _source.GetResult();
            if (result.Status == HandlingStatus.Success)
                onSuccess();

            return this;
        }

        public HandlingPromise WhenNotHandled(Action whenNotHandled)
        {
            if (!_source.IsCompleted)
            {
                // If the source has not completed immediately, then it must be being handled.
                return this;
            }

            var result = _source.GetResult();
            if (result.Status == HandlingStatus.NotHandled)
                whenNotHandled();

            return this;
        }
    }
}