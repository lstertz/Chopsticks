using System;
using System.Collections.Generic;

namespace Chopsticks.Messages.Abstractions
{
    public abstract class BaseMulticastMessageHandler<TMessage> : 
        IMessageHandlerRegistrar<TMessage>
    {
        protected class Registration : IEquatable<Registration>
        {
            public IIntercept<TMessage>[] Interceptors { get; init; }

            public int Order { get; init; } = 0;

            public IMessageHandler<TMessage> Handler { get; init; }


            public Registration(IMessageHandler<TMessage> receiver,
                params IIntercept<TMessage>[] interceptors)
            {
                Handler = receiver ?? throw new ArgumentNullException(nameof(receiver));
                Interceptors = interceptors ?? [];
            }

            /// <inheritdoc/>
            public bool Equals(Registration other) =>
                Handler.Equals(other.Handler);

            /// <inheritdoc/>
            public override int GetHashCode() =>
                Handler.GetHashCode();
        }


        // TODO :: Support registering interceptors for the multicast.
        //           Support intercepting before entire run and before each handler.
        protected IIntercept<TMessage>[] PreCollectiveRunIntercepters { get; private set; } = [];
        protected IIntercept<TMessage>[] PerReceiverRunIntercepters { get; private set; } = [];

        protected List<Registration> RegisteredHandlers { get; init; } = new(8);


        bool IMessageHandlerRegistrar<TMessage>.Register(
            IMessageHandler<TMessage> handler,
            RegistrationSettings settings = default, params IIntercept<TMessage>[] interceptors)
        {
            var registration = new Registration(handler, interceptors)
            {
                Order = settings.Order
            };

            if (RegisteredHandlers.Contains(registration))
                return false;

            RegisteredHandlers.Add(registration);
            RegisteredHandlers.Sort((x, y) =>
            {
                int orderComparison = x.Order.CompareTo(y.Order);
                if (orderComparison != 0)
                    return orderComparison;

                return x.Handler.GetHashCode().CompareTo(y.Handler.GetHashCode());
            });

            return true;
        }

        void IMessageHandlerRegistrar<TMessage>.Unregister(
            IMessageHandler<TMessage> receiver)
        {
            RegisteredHandlers.Remove(new(receiver));
        }
    }
}
