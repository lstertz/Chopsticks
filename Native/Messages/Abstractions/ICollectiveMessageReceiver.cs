namespace Chopsticks.Messages.Abstractions
{
    public interface ICollectiveMessageReceiver<TMessage, TAsync>
    {
        void Deregister(IRegisteredMessageReceiver<TMessage, TAsync> receiver);

        bool Register(IRegisteredMessageReceiver<TMessage, TAsync> receiver, 
            RegistrationSettings settings, params IIntercept<TMessage>[] interceptors);
    }
}
