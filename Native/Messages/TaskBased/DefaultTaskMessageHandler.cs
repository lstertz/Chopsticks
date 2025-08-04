using System;
using System.Threading;
using System.Threading.Tasks;

namespace Chopsticks.Messages.TaskBased
{
    // TODO :: Update for async abstraction.
    public static class DefaultTaskMessageHandler<TMessage>
    {
        // TODO :: Add an overridable builder for the default handler (supports testing and flexibility).

        private static readonly Lazy<MulticastMessageHandler<TMessage>> _defaultHandler = new();

        public static MulticastMessageHandler<TMessage> Get() =>
            _defaultHandler.Value;

        public static HandlingPromise Handle(TMessage message) =>
            _defaultHandler.Value.Handle(message);

        //public static Task<HandlingResult> HandleAsync(TMessage message,
        //    CancellationToken token = default) =>
        //    _defaultHandler.Value.HandleAsync(message, token);

        public static void Register(ISyncMessageHandler<TMessage> handler, 
            params IIntercept<TMessage>[] interceptors)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            //(_defaultHandler.Value as ITaskMessageHandlerRegistrar<TMessage>)
              //  .Register(handler, default, interceptors);
        }

        public static void Register(ISyncMessageHandler<TMessage> handler, 
            RegistrationSettings settings = default,
            params IIntercept<TMessage>[] interceptors)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            //(_defaultHandler.Value as ITaskMessageHandlerRegistrar<TMessage>)
              //  .Register(handler, settings);
        }

        public static void Unregister(ISyncMessageHandler<TMessage> handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            //(_defaultHandler.Value as ITaskMessageHandlerRegistrar<TMessage>)
              //  .Unregister(handler);
        }
    }
}
