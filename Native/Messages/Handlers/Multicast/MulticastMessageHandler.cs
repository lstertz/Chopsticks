using Chopsticks.Messages.Handlers.Sources;
using Chopsticks.Messages.Interceptors;
using Chopsticks.Messages.Registration;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Chopsticks.Messages.Handlers.Multicast;

// TODO :: Build a MulticastContextHandler that works from both message and context 
//             registrars to build its source.

public class MulticastMessageHandler<TMessage> :  
    MessageHandlerRegistrar<TMessage>,
    IMulticastMessageHandler<TMessage>
{
    // TODO :: Verify thread safety.
    private volatile Func<TMessage, CancellationToken, HandlingAwaitable> _dispatchPipeline;

    // TODO :: Support stopping at the first failure.

    public virtual HandlingPromise Handle(TMessage message) =>
        _dispatchPipeline(message, default).ToPromise();

    public virtual HandlingAwaitable HandleAsync(TMessage message,
        CancellationToken token = default) =>
            _dispatchPipeline(message, token);

    protected override void RebuildDispatchInterceptorPipeline(
        List<IMessageInterceptor<TMessage>> interceptors)
    {
        Func<TMessage, CancellationToken, HandlingAwaitable> current = 
            (message, token) => new(InitiateWithSource(message, token));

        for (int c = interceptors.Count - 1; c >= 0; c--)
        {
            var next = current;
            current = (message, token) =>
                interceptors[c].InterceptAsync(message, token, next);
        }

        _dispatchPipeline = current;
    }


    private IHandlingPromiseSource InitiateWithSource(TMessage message,
        CancellationToken token)
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
