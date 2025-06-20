namespace Chopsticks.Messages.Abstractions
{
    public interface IMessageReceiverCollective<TMessage, TAsync>
    {

        // TODO :: Accommodate clearing all.

        bool Register(IMessageReceiver<TMessage, TAsync> receiver, 
            RegistrationSettings settings, params IIntercept<TMessage>[] interceptors);

        void Unregister(IMessageReceiver<TMessage, TAsync> receiver);
    }
}
