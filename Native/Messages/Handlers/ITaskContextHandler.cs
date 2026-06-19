using Chopsticks.Messages.Handlers.Sources;
using Chopsticks.Messages.Handlers.Sources.Pooling;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Chopsticks.Messages.Handlers
{
    public interface ITaskContextHandler<TMessage, TContext> :
        IContextHandler<TMessage, TContext>
        where TContext : IMessageContext<TMessage>, new()
    {
        new Task HandleAsync(TContext context);


        HandlingCompletionPromise IContextHandler<TMessage, TContext>.Handle(TContext context,
            SynchronizationContext? asyncContext)
        {
            var awaiter = EvaluateHandleAsync(context, out var syncReturnResult);
            return syncReturnResult.Status != HandlingStatus.Processing
                ? new HandlingCompletionPromise(syncReturnResult, asyncContext)
                : new HandlingCompletionPromise(awaiter!, asyncContext);
        }

        HandlingResultPromise IContextHandler<TMessage, TContext>.TryHandle(TContext context)
        {
            var awaiter = EvaluateHandleAsync(context, out var syncReturnResult);
            return syncReturnResult.Status != HandlingStatus.Processing
                ? new HandlingResultPromise(syncReturnResult)
                : new HandlingResultPromise(awaiter!);
        }

        HandlingResultAwaitable IContextHandler<TMessage, TContext>.TryHandleAsync(TContext context)
        {
            // Maintain explicit cast to ensure dispatching to the correct method.
            Task task = (this as ITaskContextHandler<TMessage, TContext>).HandleAsync(context);

            var source = TryHandleAsyncTaskPromiseSource.Pool.Rent();
            source.Init(task.GetAwaiter());

            return new HandlingResultAwaitable(source);
        }


        private HandlingTaskAwaiter? EvaluateHandleAsync(TContext context,
            out HandlingResult syncReturnResult)
        {
            try
            {
                // Maintain explicit cast to ensure dispatching to the correct method.
                Task task = (this as ITaskContextHandler<TMessage, TContext>).HandleAsync(context);

                var awaiter = HandlingAwaiterPool.Instance.Rent<HandlingTaskAwaiter, TaskAwaiter>(
                    task.GetAwaiter());

                syncReturnResult = HandlingResult.Processing;
                return awaiter;
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
        }

    }
}
