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
        var defaultContext = new DefaultMessageContext<TMessage>
        {
            CancellationToken = token,
            Message = message
        };
        
        // TODO :: Rent from the pool.
        var source = new SequentialHandlingPromiseSource<TMessage, DefaultMessageContext<TMessage>>();
        source.Init(RegisteredMessageHandlers);
        source.Run(defaultContext);

        return source;
    }
}
