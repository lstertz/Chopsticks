using System;
using System.Collections.Generic;

namespace Chopsticks.Messages.Abstractions
{
    public abstract class BaseCollectiveMessageReceiver<TReceiver, TAsyncReceiver, TMessage, TAsync> : 
        IBaseCollectiveMessageReceiver<TReceiver, TMessage>,
        IBaseCollectiveAsyncMessageReceiver<TAsyncReceiver, TMessage, TAsync>
        where TReceiver : IBaseMessageReceiver<TMessage>
        where TAsyncReceiver : IBaseAsyncMessageReceiver<TMessage, TAsync>
    {
        protected List<TReceiver> Receivers { get; init; } = [];
        protected List<TAsyncReceiver> AsyncReceivers { get; init; } = [];


        public void Deregister(TReceiver handler)
        {
            // TODO :: Deregister.
        }

        public void Deregister(TAsyncReceiver handler)
        {
            // TODO :: Deregister.
        }


        public void Register(TReceiver receiver, RegistrationSettings settings = default) =>
            Register(receiver, settings, []);

        public void Register(TReceiver receiver, params IIntercept<TMessage>[] interceptors) =>
            Register(receiver, default, interceptors);

        public void Register(TReceiver receiver, RegistrationSettings settings,
            params IIntercept<TMessage>[] interceptors)
        {
            // TODO :: Register.
        }


        public void Register(TAsyncReceiver receiver, RegistrationSettings settings = default) =>
            Register(receiver, settings, []);

        public void Register(TAsyncReceiver receiver, params IIntercept<TMessage>[] interceptors) =>
            Register(receiver, default, interceptors);

        public void Register(TAsyncReceiver receiver, RegistrationSettings settings,
            params IIntercept<TMessage>[] interceptors)
        {
            // TODO :: Register.
        }
    }
}
