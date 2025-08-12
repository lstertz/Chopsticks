using Chopsticks.Messages.Handlers;
using Chopsticks.Messages.Interceptors;
using System;
using System.Collections.Generic;

namespace Chopsticks.Messages.Registration;

public abstract class MessageHandlerRegistrar<TMessage> :
    IMessageHandlerRegistrar<TMessage>
{
    protected class MessageHandlerRegistration : IEquatable<MessageHandlerRegistration>
    {
        public int Order { get; init; } = 0;

        public IMessageHandler<TMessage> Handler { get; init; }


        public MessageHandlerRegistration(IMessageHandler<TMessage> handler)
        {
            Handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        /// <inheritdoc/>
        public bool Equals(MessageHandlerRegistration other) =>
            Handler.Equals(other.Handler);

        /// <inheritdoc/>
        public override int GetHashCode() =>
            Handler.GetHashCode();
    }


    // TODO :: Support registering interceptors for the multicast.
    // TODO :: Rebuild immutable collection used during handling on any register/unregister.
    protected IMessageInterceptor<TMessage>[] MulticastMessageInterceptors =>
        [.. _multicastMessageInterceptors];
    private readonly List<IMessageInterceptor<TMessage>> _multicastMessageInterceptors = [];

    protected IMessageInterceptor<TMessage>[] PerHandlerMessageInterceptors =>
        [.. _perHandlerMessageInterceptors];
    private readonly List<IMessageInterceptor<TMessage>> _perHandlerMessageInterceptors = [];

    // TODO :: Rebuild immutable collection used during handling on any register/unregister.
    protected MessageHandlerRegistration[] RegisteredMessageHandlers => [.. _registeredMessageHandlers];
    private readonly List<MessageHandlerRegistration> _registeredMessageHandlers = new(8);


    bool IMessageHandlerRegistrar<TMessage>.Register(
        IMessageHandler<TMessage> handler,
        RegistrationSettings settings)
    {
        var registration = new MessageHandlerRegistration(handler)
        {
            Order = settings.Order
        };

        if (_registeredMessageHandlers.Contains(registration))
            return false;

        _registeredMessageHandlers.Add(registration);
        _registeredMessageHandlers.Sort((x, y) =>
        {
            int orderComparison = x.Order.CompareTo(y.Order);
            if (orderComparison != 0)
                return orderComparison;

            return x.Handler.GetHashCode().CompareTo(y.Handler.GetHashCode());
        });

        return true;
    }

    void IMessageHandlerRegistrar<TMessage>.Unregister(
        IMessageHandler<TMessage> handler)
    {
        _registeredMessageHandlers.Remove(new(handler));
    }
}
