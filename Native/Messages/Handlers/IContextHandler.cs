namespace Chopsticks.Messages.Handlers
{
    public interface IContextHandler<TMessage, TContext>
        where TContext : IMessageContext<TMessage>
    {
        HandlingPromise Handle(TContext context);

        HandlingAwaitable HandleAsync(TContext context);
    }

    // TODO :: Make sync/async implementations.
}
