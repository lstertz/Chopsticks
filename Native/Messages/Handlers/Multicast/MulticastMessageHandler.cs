using Chopsticks.Messages.Handlers.Sources;
using Chopsticks.Messages.Registration;
using System.Threading;

namespace Chopsticks.Messages.Handlers.Multicast;

// TODO :: Build a MulticastContextHandler that works from both message and context 
//             registrars to build its source.

public class MulticastMessageHandler<TMessage> :  
    MessageHandlerRegistrar<TMessage>,
    IMulticastMessageHandler<TMessage>
{
    // TODO :: Support stopping at the first failure.

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
        var handlers = new IMessageHandler<TMessage>[RegisteredMessageHandlers.Length];
        for (int c = 0, count = RegisteredMessageHandlers.Length; c < count; c++)
            handlers[c] = RegisteredMessageHandlers[c].Handler;

        // TODO :: Rent from the pool.
        var source = new SequentialHandlingPromiseSource<TMessage>();
        source.Init(handlers);
        source.Run(message, token);

        return source;
    }
}
