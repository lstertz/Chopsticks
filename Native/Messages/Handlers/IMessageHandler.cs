using System.Threading;

namespace Chopsticks.Messages.Handlers
{
    // TODO :: Consider a higher-level interface that abstracts from whether 
    //             what is being invoked is a message handler, context handler, 
    //             message interceptor, or context interceptor.
    //         Likely something with HandleAsync that uses a generalized struct, 
    //             like SourceExecutionContext, which should have context (somehow) 
    //             the message, and a next() callback (for interceptors).

    public interface IMessageHandler<TMessage>
    {
        HandlingPromise Handle(TMessage message);

        HandlingAwaitable HandleAsync(TMessage message, CancellationToken token = default);
    }
}
