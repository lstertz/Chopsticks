namespace Chopsticks.Messages.Abstractions
{
    public static class IMessageHandlerRegistrarExtensions
    {
        public static bool Register<TMessage, TAsync>(
            this IMessageHandlerRegistrar<TMessage, TAsync> registrar,
            IMessageHandler<TMessage, TAsync> receiver,
            RegistrationSettings settings = default) => 
                registrar.Register(receiver, settings);

        public static bool Register<TMessage, TAsync>(
            this IMessageHandlerRegistrar<TMessage, TAsync> registrar,
            IMessageHandler<TMessage, TAsync> receiver,
            params IIntercept<TMessage>[] interceptors) => 
                registrar.Register(receiver, default, interceptors);
    }
}
