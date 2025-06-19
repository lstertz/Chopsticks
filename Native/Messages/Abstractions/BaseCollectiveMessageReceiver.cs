using System;
using System.Collections.Generic;

namespace Chopsticks.Messages.Abstractions
{
    public abstract class BaseCollectiveMessageReceiver<TMessage, TAsync> : 
        ICollectiveMessageReceiver<TMessage, TAsync>
    {
        protected class Registration : IEquatable<Registration>
        {
            public IIntercept<TMessage>[] Interceptors { get; init; }

            public int Order { get; init; } = 0;

            public IRegisteredMessageReceiver<TMessage, TAsync> Receiver { get; init; }


            public Registration(IRegisteredMessageReceiver<TMessage, TAsync> receiver,
                params IIntercept<TMessage>[] interceptors)
            {
                Receiver = receiver ?? throw new ArgumentNullException(nameof(receiver));
                Interceptors = interceptors ?? [];
            }

            /// <inheritdoc/>
            public bool Equals(Registration other) =>
                Receiver.Equals(other.Receiver);

            /// <inheritdoc/>
            public override int GetHashCode() =>
                Receiver.GetHashCode();
        }


        // TODO :: Support registering interceptors for the collective.
        //           Support intercepting before entire run and before each receiver.
        protected IIntercept<TMessage>[] PreCollectiveRunIntercepters { get; private set; } = [];
        protected IIntercept<TMessage>[] PerReceiverRunIntercepters { get; private set; } = [];

        protected List<Registration> RegisteredReceivers { get; init; } = new(8);


        public virtual void Deregister(IRegisteredMessageReceiver<TMessage, TAsync> receiver)
        {
            RegisteredReceivers.Remove(new(receiver));
        }

        public virtual bool Register(IRegisteredMessageReceiver<TMessage, TAsync> receiver,
            RegistrationSettings settings, params IIntercept<TMessage>[] interceptors)
        {
            var registration = new Registration(receiver, interceptors);
            if (RegisteredReceivers.Contains(registration))
                return false;

            RegisteredReceivers.Add(registration);
            RegisteredReceivers.Sort((x, y) =>
            {
                int orderComparison = x.Order.CompareTo(y.Order);
                if (orderComparison != 0)
                    return orderComparison;

                return x.Receiver.GetHashCode().CompareTo(y.Receiver.GetHashCode());
            });

            return true;
        }
    }
}
