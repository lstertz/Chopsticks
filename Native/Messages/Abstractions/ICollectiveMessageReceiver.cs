namespace Chopsticks.Messages.Abstractions
{
    public interface ICollectiveMessageReceiver<TMessage, TAsync>
    {
        void Deregister(IRegisteredMessageReceiver<TMessage, TAsync> handler);

        void Register(IRegisteredMessageReceiver<TMessage, TAsync> handler, 
            RegistrationSettings settings = default);

        public void Register(IRegisteredMessageReceiver<TMessage, TAsync> receiver, 
            params IIntercept<TMessage>[] interceptors);

        public void Register(IRegisteredMessageReceiver<TMessage, TAsync> receiver, 
            RegistrationSettings settings, params IIntercept<TMessage>[] interceptors);
    }
}
