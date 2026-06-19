using Chopsticks.Messages.Handlers.Sources;
using Chopsticks.Messages.Handlers.Sources.Pooling;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Chopsticks.Messages.Handlers
{
    public interface ITaskMessageHandler<TMessage> :
        IMessageHandler<TMessage>
    {
        new Task HandleAsync(TMessage message, CancellationToken token = default);


        HandlingCompletionPromise IMessageHandler<TMessage>.Handle(TMessage message, 
            SynchronizationContext? asyncContext)
        {
            var awaiter = EvaluateHandleAsync(message, out var syncReturnResult);
            return syncReturnResult.Status != HandlingStatus.Processing
                ? new HandlingCompletionPromise(syncReturnResult, asyncContext)
                : new HandlingCompletionPromise(awaiter!, asyncContext);
        }

        HandlingResultPromise IMessageHandler<TMessage>.TryHandle(TMessage message)
        {
            var awaiter = EvaluateHandleAsync(message, out var syncReturnResult);
            return syncReturnResult.Status != HandlingStatus.Processing
                ? new HandlingResultPromise(syncReturnResult)
                : new HandlingResultPromise(awaiter!);
        }

        HandlingResultAwaitable IMessageHandler<TMessage>.TryHandleAsync(
            TMessage message, CancellationToken token)
        {
            // Maintain explicit cast to ensure dispatching to the correct method.
            Task task = (this as ITaskMessageHandler<TMessage>).HandleAsync(message, token);

            var source = TryHandleAsyncTaskPromiseSource.Pool.Rent();
            source.Init(task.GetAwaiter());

            return new HandlingResultAwaitable(source);
        }


        private HandlingTaskAwaiter? EvaluateHandleAsync(TMessage message, 
            out HandlingResult syncReturnResult)
        {
            Task task;
            try
            {
                // Maintain explicit cast to ensure dispatching to the correct method.
                task = (this as ITaskMessageHandler<TMessage>).HandleAsync(message);
            }
            catch (OperationCanceledException)
            {
                syncReturnResult = HandlingResult.Cancelled;
                return default;
            }
            catch (Exception ex)
            {
                syncReturnResult = HandlingResult.FromException(ex);
                return default;
            }


            if (task.IsCompleted)
            {
                if (task.IsCanceled)
                {
                    syncReturnResult = HandlingResult.Cancelled;
                }
                else if (task.IsFaulted)
                {
                    syncReturnResult = HandlingResult.FromException(task.Exception!);
                }
                else
                {
                    syncReturnResult = HandlingResult.Success;
                }
                return default;
            }

            var awaiter = HandlingAwaiterPool.Instance.Rent<HandlingTaskAwaiter, TaskAwaiter>(
                task.GetAwaiter());

            syncReturnResult = HandlingResult.Processing;
            return awaiter;
        }
    }
}
