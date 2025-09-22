using Chopsticks.Messages.Handlers.Sources;
using Chopsticks.Messages.Registration;
using Chopsticks.Messages.Registration.Interceptors;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Chopsticks.Messages.Handlers.Multicast;

public abstract class BaseMulticastHandler<TMessage, TContext> :
    BaseMessageHandlerRegistrar<TMessage, TContext>
    where TContext : IMessageContext<TMessage>, new()
{
    // TODO :: Verify thread safety.
    protected volatile Func<TContext, HandlingAwaitable> _dispatchPipeline;
    // TODO :: Support stopping at the first failure.

    protected override void RebuildDispatchPipeline(
        List<RegisteredInterceptor<TMessage, TContext>> interceptors)
    {
        Func<TContext, HandlingAwaitable> current =
            (context) => new(InitiateWithSource(context.Message, context.CancellationToken));

        for (int c = interceptors.Count - 1; c >= 0; c--)
        {
            var next = current;
            var interceptor = interceptors[c];
            current = (context) =>
                interceptor.InterceptAsync(context, next);
        }

        _dispatchPipeline = current;
    }

    protected IHandlingPromiseSource InitiateWithSource(TMessage message,
        CancellationToken token)
    {
        var defaultContext = new TContext
        {
            CancellationToken = token,
            Message = message
        };

        // TODO :: Rent from the pool.
        var source = new SequentialHandlingPromiseSource<TMessage, TContext>();
        source.Init(RegisteredMessageHandlers);
        source.Run(defaultContext);
        return source;
    }
}
