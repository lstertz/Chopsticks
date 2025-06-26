namespace Chopsticks.Messages.Abstractions
{
    public interface IMulticastMessageRegistrar<TMessage, TAsync>
    {
        // TODO :: Accommodate clearing all.
        bool Register(IMessageHandler<TMessage, TAsync> handler, 
            RegistrationSetting settings, params IIntercept<TMessage>[] interceptors);

        void Unregister(IMessageHandler<TMessage, TAsync> handler);
    }
}
