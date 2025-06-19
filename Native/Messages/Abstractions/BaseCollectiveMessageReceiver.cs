using System.Collections.Generic;

namespace Chopsticks.Messages.Abstractions
{
    public abstract class BaseCollectiveMessageReceiver<TMessage, TAsync> : 
        ICollectiveMessageReceiver<TMessage, TAsync>
    {
        // TODO :: Support registering interceptors for the collective.
        //           Support intercepting before entire run and before each receiver.
        protected IIntercept<TMessage>[] Intercepters { get; private set; } = [];

        protected List<IRegisteredMessageReceiver<TMessage, TAsync>> Receivers { get; init; } = [];


        public void Deregister(IRegisteredMessageReceiver<TMessage, TAsync> handler)
        {
            // TODO :: Deregister.
        }


        public void Register(IRegisteredMessageReceiver<TMessage, TAsync> receiver, RegistrationSettings settings = default) =>
            Register(receiver, settings, []);

        public void Register(IRegisteredMessageReceiver<TMessage, TAsync> receiver, params IIntercept<TMessage>[] interceptors) =>
            Register(receiver, default, interceptors);

        public void Register(IRegisteredMessageReceiver<TMessage, TAsync> receiver, RegistrationSettings settings,
            params IIntercept<TMessage>[] interceptors)
        {
            // TODO :: Register.
        }
    }
}
