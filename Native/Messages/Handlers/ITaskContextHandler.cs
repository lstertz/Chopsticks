using Chopsticks.Messages.Handlers.Sources;
using System.Threading.Tasks;

namespace Chopsticks.Messages.Handlers
{
    public interface ITaskContextHandler<TMessage, TContext> :
        IContextHandler<TMessage, TContext>
        where TContext : IMessageContext<TMessage>, new()
    {
        new Task HandleAsync(TContext context);


        HandlingResultPromise IContextHandler<TMessage, TContext>.TryHandle(TContext context)
        {
            var source = TryHandleAsyncTaskPromiseSource.Pool.Rent();
            source.Init(HandleAsync(context).GetAwaiter());

            return new HandlingResultPromise(source);
        }

        HandlingResultAwaitable IContextHandler<TMessage, TContext>.TryHandleAsync(TContext context)
        {
            // Maintain explicit cast to ensure dispatching to the correct method.
            Task task = (this as ITaskContextHandler<TMessage, TContext>).HandleAsync(context);

            var source = TryHandleAsyncTaskPromiseSource.Pool.Rent();
            source.Init(task.GetAwaiter());

            return new HandlingResultAwaitable(source);
        }

    }
}
