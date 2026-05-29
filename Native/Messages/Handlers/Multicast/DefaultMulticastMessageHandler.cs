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

        public static HandlingResultPromise TryHandle(TMessage message) =>
            _defaultHandler.Value.TryHandle(message);

        public static HandlingResultAwaitable TryHandleAsync(TMessage message,
            CancellationToken token = default) =>
            _defaultHandler.Value.TryHandleAsync(message, token);
        
        /// <summary>
        /// Dispatches the message to all handlers without returning a result.
        /// Zero allocation fire-and-forget pattern - exceptions are swallowed.
        /// </summary>
        /// <remarks>
        /// Use this when you don't need the handling result and want optimal performance.
        /// For error handling, use <see cref="HandleSync"/> instead.
        /// </remarks>
        public static void TryHandleFireAndForget(TMessage message, 
            CancellationToken token = default) =>
            _defaultHandler.Value.TryHandleFireAndForget(message, token);
        
        /// <summary>
        /// Dispatches the message to all handlers synchronously and returns the result.
        /// Zero allocation when all handlers are synchronous.
        /// </summary>
        /// <returns>The aggregated handling result from all handlers.</returns>
        public static HandlingResult HandleSync(TMessage message, 
            CancellationToken token = default) =>
            _defaultHandler.Value.HandleSync(message, token);

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
