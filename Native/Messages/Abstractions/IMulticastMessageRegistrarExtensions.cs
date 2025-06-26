namespace Chopsticks.Messages.Abstractions
{
    public static class IMulticastMessageRegistrarExtensions
    {
        public static bool Register<TMessage, TAsync>(
            this IMulticastMessageRegistrar<TMessage, TAsync> registrar,
            IMessageHandler<TMessage, TAsync> receiver,
            RegistrationSettings settings = default) => 
                registrar.Register(receiver, settings);

        public static bool Register<TMessage, TAsync>(
            this IMulticastMessageRegistrar<TMessage, TAsync> registrar,
            IMessageHandler<TMessage, TAsync> receiver,
            params IIntercept<TMessage>[] interceptors) => 
                registrar.Register(receiver, default, interceptors);
    }
}
