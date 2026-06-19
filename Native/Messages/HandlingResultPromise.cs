using Chopsticks.Messages.Handlers.Sources.Pooling;
using System;
using System.Collections.Generic;

namespace Chopsticks.Messages
{
    public readonly struct HandlingResultPromise : IDisposable
    {
        private const string DisposalExceptionMessage =
            "The promise has been released and can no longer be used.";


        public static HandlingResultPromise NoHandlers => new(HandlingResult.NoHandlers);
        public static HandlingResultPromise Success => new(HandlingResult.Success);


        /// <summary>
        /// Gets the last known result status of the message handling operation.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown if the source 
        /// of the promise has already been released through <see cref="Release"/> 
        /// and a cached result after completion is not available.</exception>
        public readonly HandlingResult Result => GetResult();

        private readonly bool HasBeenReleased =>
            _awaiter != null && _awaiter.Version != _awaiterVersion;

        private readonly HandlingResult _result = HandlingResult.Processing;

        private readonly IHandlingAwaiter? _awaiter;
        private readonly int _awaiterVersion;


        public HandlingResultPromise(HandlingResult result)
        {
            if (result.Status == HandlingStatus.Processing)
                throw new ArgumentException("A pre-completed promise cannot have a " +
                    "result that is \"Processing\".", nameof(result));

            _result = result;
            _awaiter = null;
        }

        public HandlingResultPromise(IHandlingAwaiter resultAwaiter)
        {
            if (resultAwaiter.IsCompleted)  // Async awaitable has completed synchronously.
            {
                _result = resultAwaiter.GetResult();
                resultAwaiter.CanRelease = true;

                _awaiter = null;

                return;
            }

            _awaiter = resultAwaiter;
            _awaiterVersion = resultAwaiter.Version;
        }

        /// <inheritdoc/>
        void IDisposable.Dispose() => Release();

        /// <summary>
        /// Releases this promise by returning its inner source to a pool. 
        /// This should be done once all operations on all copies of this struct 
        /// have been completed.
        /// </summary>
        /// <remarks>
        /// Any further operations (e.g., continuation calls or use of <see cref="Result"/>) 
        /// will throw an <see cref="ObjectDisposedException"/>.
        /// </remarks>
        public void Release()
        {
            if (_awaiter != null)
                _awaiter.CanRelease = true;
        }


        public HandlingResultPromise OnCancelled(Action onCancelled)
        {
            var status = GetResult().Status;
            if (status != HandlingStatus.Processing)
            {
                if (status == HandlingStatus.Cancelled)
                    onCancelled();
                return this;
            }

            _awaiter!.OnCancelled = onCancelled;
            return this;
        }

        public HandlingResultPromise OnCompletion(Action<HandlingResult> onCompletion)
        {
            var result = GetResult();
            if (result.Status != HandlingStatus.Processing)
            {
                if ((result.Status & HandlingStatus.Completed) != 0)
                    onCompletion(result);
                return this;
            }

            _awaiter!.OnCompletionWithResult = onCompletion;
            return this;
        }

        public HandlingResultPromise OnFailure(Action<IEnumerable<Exception>> onFailure)
        {
            var result = GetResult();
            if (result.Status != HandlingStatus.Processing)
            {
                if (result.Status == HandlingStatus.Failure)
                    onFailure(result.Exceptions);
                return this;
            }

            _awaiter!.OnFailure = onFailure;
            return this;
        }

        public HandlingResultPromise OnNonSuccess(Action<HandlingResult> onNonSuccess)
        {
            var result = GetResult();
            if (result.Status != HandlingStatus.Processing)
            {
                if ((result.Status & HandlingStatus.NonSuccess) != 0)
                    onNonSuccess(result);
                return this;
            }

            _awaiter!.OnNonSuccess = onNonSuccess;
            return this;
        }

        public HandlingResultPromise OnSuccess(Action onSuccess)
        {
            var status = GetResult().Status;
            if (status != HandlingStatus.Processing)
            {
                if (status == HandlingStatus.Success)
                    onSuccess();
                return this;
            }

            _awaiter!.OnSuccess = onSuccess;
            return this;
        }

        public HandlingResultPromise WhenNotHandled(Action whenNotHandled)
        {
            var status = GetResult().Status;
            if (status == HandlingStatus.Processing)
            {
                // If we don't have a completed result, then it must be being handled.
                return this;
            }

            if (status == HandlingStatus.NotHandled)
                whenNotHandled();

            return this;
        }


        private readonly HandlingResult GetResult()
        {
            if (_awaiter == null)
                return _result;

            if (HasBeenReleased)
            {
                throw new ObjectDisposedException(nameof(HandlingCompletionPromise),
                    DisposalExceptionMessage);
            }

            return _awaiter.GetResult();
        }
    }
}