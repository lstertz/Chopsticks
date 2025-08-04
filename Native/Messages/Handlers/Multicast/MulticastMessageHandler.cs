using Chopsticks.Messages.Handlers.Sources;
using Chopsticks.Messages.Interception;
using Chopsticks.Messages.Registration;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Chopsticks.Messages.Handlers.Multicast
{
    public class MulticastMessageHandler<TMessage> : 
        IMessageHandlerRegistrar<TMessage>,
        IMulticastMessageHandler<TMessage>
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

        // TODO :: Support stopping at the first failure.

        // TODO :: Support registering interceptors for the multicast.
        //           Support intercepting before entire run and before each handler.
        protected IIntercept<TMessage>[] PreCollectiveRunIntercepters { get; private set; } = [];
        protected IIntercept<TMessage>[] PerReceiverRunIntercepters { get; private set; } = [];

        protected List<Registration> RegisteredHandlers { get; init; } = new(8);


        public virtual HandlingPromise Handle(TMessage message) =>
            new(InitiateWithSource(message));

        public virtual HandlingAwaitable HandleAsync(TMessage message,
            CancellationToken token = default) =>
                new(InitiateWithSource(message, token));


        private IHandlingPromiseSource InitiateWithSource(TMessage message,
            CancellationToken token = default)
        {
            // Build handler collection.
            // TODO :: Do this on registration/unregistration instead.
            var handlers = new IMessageHandler<TMessage>[RegisteredHandlers.Count];
            for (int c = 0, count = RegisteredHandlers.Count; c < count; c++)
                handlers[c] = RegisteredHandlers[c].Handler;

            // TODO :: Rent from the pool.
            var source = new SequentialHandlingPromiseSource<TMessage>();
            source.Init(handlers);
            source.Run(message, token);

            return source;
        }


        // TODO :: Rebuild immutable collection used during handling on any register/unregister.
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
