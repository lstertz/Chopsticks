using Chopsticks.Messages.Interceptors;
using Chopsticks.Messages.Registration;
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

        public static HandlingPromise Handle(TMessage message) =>
            _defaultHandler.Value.Handle(message);

        public static HandlingAwaitable HandleAsync(TMessage message,
            CancellationToken token = default) =>
            _defaultHandler.Value.HandleAsync(message, token);

        public static void Register(ISyncMessageHandler<TMessage> handler, 
            params IMessageInterceptor<TMessage>[] interceptors)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            (_defaultHandler.Value as IHandlerRegistrar<IMessageHandler<TMessage>, IMessageInterceptor<TMessage>>)
                .Register(handler, default, interceptors);
        }

        public static void Register(ISyncMessageHandler<TMessage> handler, 
            RegistrationSettings settings = default,
            params IMessageInterceptor<TMessage>[] interceptors)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            (_defaultHandler.Value as IHandlerRegistrar<IMessageHandler<TMessage>, IMessageInterceptor<TMessage>>)
                .Register(handler, settings,interceptors);
        }

        public static void Unregister(ISyncMessageHandler<TMessage> handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            (_defaultHandler.Value as IHandlerRegistrar<IMessageHandler<TMessage>, IMessageInterceptor<TMessage>>)
                .Unregister(handler);
        }
    }
}
