using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Chopsticks.Messages
{
    public class TestClass
    {
        public async Task Test()
        {
            async ValueTask TestAsync()
            {

            }

            var source = new TaskHandlingPromiseSource();
            source.Init(Task.CompletedTask.GetAwaiter());

            var awaiter = new HandlingAwaitable(source);
            var t = await awaiter.ContinueWithTask()
                .WhenCancelledAsync(async () =>
                {
                    await TestAsync();
                })
                .WhenSuccessfulAsync(async () =>
                {
                    await Task.Delay(1000);
                });

            var source2 = new SyncHandlingPromiseSource();
            source2.Init(HandlingResult.Success);

            var t2 = new HandlingPromise(source2)
                .OnCancelled(() => Console.WriteLine("Cancelled"))
                .OnCompletion(result => Console.WriteLine($"Completed with status: {result.Status}"))
                .OnFailure(exceptions => Console.WriteLine($"Failed with exceptions: {string.Join(", ", exceptions)}"))
                .OnNonSuccess(result => Console.WriteLine($"Non-success result: {result.Status}"))
                .OnSuccess(() => Console.WriteLine("Success"))
                .WhenNotHandled(() => Console.WriteLine("Not handled"));
        }
    }

    public interface IHandlingPromiseSource
    {
        public Action InitiateDefaultContinuations { get; }

        public Action? OnCancelled { get; set; }
        public Action<HandlingResult>? OnCompletion { get; set; }
        public Action<IEnumerable<Exception>>? OnFailure { get; set; }
        public Action<HandlingResult>? OnNonSuccess { get; set; }
        public Action? OnSuccess { get; set; }

        bool IsCompleted { get; }
        HandlingResult GetResult();
        void OnCompleted(Action continuation);
    }

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

    public abstract class BaseHandlingPromiseSource<TInnerSource> : IHandlingPromiseSource
        where TInnerSource : struct
    {
        public abstract bool IsCompleted { get; }

        public Action InitiateDefaultContinuations { get; private set; }

        public Action? OnCancelled { get; set; }
        public Action<HandlingResult>? OnCompletion { get; set; }
        public Action<IEnumerable<Exception>>? OnFailure { get; set; }
        public Action<HandlingResult>? OnNonSuccess { get; set; }
        public Action? OnSuccess { get; set; }


        protected TInnerSource InnerSource { get; private set; }
        private bool _isInitialized = false;


        public BaseHandlingPromiseSource()
        {
            InitiateDefaultContinuations = () =>
            {
                VerifyInitialized();
                var result = GetResult();

                if (result.Status == HandlingStatus.Cancelled)
                    OnCancelled?.Invoke();
                else if ((result.Status & HandlingStatus.Completed) != 0)
                    OnCompletion?.Invoke(result);
                else if (result.Status == HandlingStatus.Failure)
                    OnFailure?.Invoke(result.Exceptions);
                else if ((result.Status & HandlingStatus.NonSuccess) != 0)
                    OnNonSuccess?.Invoke(result);
                else if (result.Status == HandlingStatus.Success)
                    OnSuccess?.Invoke();
            };
        }

        public IHandlingPromiseSource Init(TInnerSource innerSource)
        {
            _isInitialized = true;
            InnerSource = innerSource;

            return this;
        }

        public void Dispose()
        {
            InnerSource = default;

            _isInitialized = false;

            OnCancelled = null;
            OnCompletion = null;
            OnFailure = null;
            OnNonSuccess = null;
            OnSuccess = null;
        }


        public abstract HandlingResult GetResult();

        public abstract void OnCompleted(Action continuation);


        protected void VerifyInitialized()
        {
            if (!_isInitialized)
                throw new InvalidOperationException(
                    "The promise source either has not been initialized or has been disposed.");
        }
    }


    // TODO :: Create sources to wrap other sources, including to sequentially 
    //           step through multiple sources.
    public class SyncHandlingPromiseSource :
        BaseHandlingPromiseSource<HandlingResult>
    {
        public override bool IsCompleted => true;


        public override HandlingResult GetResult() => InnerSource;

        public override void OnCompleted(Action continuation) => 
            continuation();
    }

    // TODO :: Implement pooling for this source.
    public class TaskHandlingPromiseSource : 
        BaseHandlingPromiseSource<TaskAwaiter>
    {
        public override bool IsCompleted => 
            InnerSource.IsCompleted;


        public override HandlingResult GetResult()
        {
            VerifyInitialized();

            try
            {
                InnerSource.GetResult();
                return HandlingResult.Success;
            }
            catch (OperationCanceledException)
            {
                return HandlingResult.Cancelled;
            }
            catch (Exception ex)
            {
                return HandlingResult.FromException(ex);
            }
        }

        public override void OnCompleted(Action continuation)
        {
            VerifyInitialized();
            InnerSource.OnCompleted(continuation);
        }
    }
}