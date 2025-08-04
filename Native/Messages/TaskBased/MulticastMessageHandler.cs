using Chopsticks.Messages.Abstractions;
using System.Threading;

namespace Chopsticks.Messages.TaskBased
{
    // TODO :: Merge into the base multicast handler.
    public class MulticastMessageHandler<TMessage> :
        BaseMulticastMessageHandler<TMessage>,
        IMessageHandler<TMessage>
    {
        public virtual HandlingPromise Handle(TMessage message) =>
            new(InitiateWithSource(message));

        public virtual HandlingAwaitable HandleAsync(TMessage message,
            CancellationToken token = default) => 
                new(InitiateWithSource(message, token));


        private IHandlingPromiseSource InitiateWithSource(TMessage message, 
            CancellationToken token = default)
        {
            // Build handler collection.
            // TODO :: Do this on registration/unregistration instead.
            var handlers = new IMessageHandler<TMessage>[RegisteredHandlers.Count];
            for (int c = 0, count = RegisteredHandlers.Count; c < count; c++)
                handlers[c] = RegisteredHandlers[c].Handler;

            // TODO :: Rent from the pool.
            var source = new SequentialHandlingPromiseSource<TMessage>();
            source.Init(handlers);
            source.Run(message, token);

            return source;
        }
    }
}
