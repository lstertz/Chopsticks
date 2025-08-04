using Chopsticks.Messages.Abstractions;
using System;
using System.Threading;

namespace Chopsticks.Messages
{
    public static class DefaultMulticastMessageHandler<TMessage>
    {
        // TODO :: Add an overridable builder for the default handler (supports testing and flexibility).

        private static readonly Lazy<MulticastMessageHandler<TMessage>> _defaultHandler = new();

        public static MulticastMessageHandler<TMessage> Get() =>
            _defaultHandler.Value;

        public static HandlingPromise Handle(TMessage message) =>
            _defaultHandler.Value.Handle(message);

        public static HandlingAwaitable HandleAsync(TMessage message,
            CancellationToken token = default) =>
            _defaultHandler.Value.HandleAsync(message, token);

        public static void Register(ISyncMessageHandler<TMessage> handler, 
            params IIntercept<TMessage>[] interceptors)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            (_defaultHandler.Value as IMessageHandlerRegistrar<TMessage>)
                .Register(handler, default, interceptors);
        }

        public static void Register(ISyncMessageHandler<TMessage> handler, 
            RegistrationSettings settings = default,
            params IIntercept<TMessage>[] interceptors)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            (_defaultHandler.Value as IMessageHandlerRegistrar<TMessage>)
                .Register(handler, settings);
        }

        public static void Unregister(ISyncMessageHandler<TMessage> handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            (_defaultHandler.Value as IMessageHandlerRegistrar<TMessage>)
                .Unregister(handler);
        }
    }
}
