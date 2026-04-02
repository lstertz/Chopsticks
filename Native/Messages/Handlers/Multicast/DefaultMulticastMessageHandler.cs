using Chopsticks.Messages.Registration;
using Chopsticks.Messages.Registration.Handlers;
using System;
using System.Threading;

namespace Chopsticks.Messages.Handlers.Multicast
{
    public static class DefaultMulticastMessageHandler<TMessage>
    {
        // TODO :: Add an overridable builder for the default handler (supports testing and flexibility).

        private static readonly Lazy<MulticastMessageHandler<TMessage>> _defaultHandler = new();

        public static MulticastMessageHandler<TMessage> Get() =>
            _defaultHandler.Value;

        public static HandlingPromise TryHandle(TMessage message) =>
            _defaultHandler.Value.TryHandle(message);

        public static HandlingAwaitable TryHandleAsync(TMessage message,
            CancellationToken token = default) =>
            _defaultHandler.Value.TryHandleAsync(message, token);

        public static void Register(ISyncMessageHandler<TMessage> handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            (_defaultHandler.Value as IMessageHandlerRegistrar<TMessage>)
                .Register(handler, default);
        }

        public static void Register(ISyncMessageHandler<TMessage> handler, 
            HandlerRegistrationSettings settings)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            (_defaultHandler.Value as IMessageHandlerRegistrar<TMessage>)
                .Register(handler, settings);
        }

        public static void Unregister(IRegisteredHandler registration)
        {
            (_defaultHandler.Value as IMessageHandlerRegistrar<TMessage>)
                .Unregister(registration);
        }
    }
}
