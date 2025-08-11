using Chopsticks.Messages.Registration;
using System;
using System.Collections.Generic;

namespace Chopsticks.Messages.Handlers.Multicast
{
    public abstract class MulticastRegistrar<THandler, TInterceptor> :
        IMessageHandlerRegistrar<THandler, TInterceptor>
    {
        protected class Registration : IEquatable<Registration>
        {
            public TInterceptor[] Interceptors { get; init; }

            public int Order { get; init; } = 0;

            public THandler Handler { get; init; }


            public Registration(THandler handler,
                params TInterceptor[] interceptors)
            {
                Handler = handler ?? throw new ArgumentNullException(nameof(handler));
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
        protected TInterceptor[] PreCollectiveRunIntercepters { get; private set; } = [];
        protected TInterceptor[] PerReceiverRunIntercepters { get; private set; } = [];

        protected List<Registration> RegisteredHandlers { get; init; } = new(8);


        // TODO :: Rebuild immutable collection used during handling on any register/unregister.
        bool IMessageHandlerRegistrar<THandler, TInterceptor>.Register(
            THandler handler,
            RegistrationSettings settings = default, params TInterceptor[] interceptors)
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

        void IMessageHandlerRegistrar<THandler, TInterceptor>.Unregister(
            THandler handler)
        {
            RegisteredHandlers.Remove(new(handler));
        }
    }
}
