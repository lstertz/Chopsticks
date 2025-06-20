namespace Chopsticks.Messages.Abstractions
{
    public interface ICollectiveMessageReceiver<TMessage, TAsync>
    {

        // TODO :: Accommodate clearing all.

        void Deregister(IRegisteredMessageReceiver<TMessage, TAsync> receiver);

        bool Register(IRegisteredMessageReceiver<TMessage, TAsync> receiver, 
            RegistrationSettings settings, params IIntercept<TMessage>[] interceptors);
    }
}
