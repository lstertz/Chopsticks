namespace Chopsticks.Messages.Abstractions
{
    public static class IMessageHandlerRegistrarExtensions
    {
        public static bool Register<TMessage, TAsync>(
            this IMessageHandlerRegistrar<TMessage, TAsync> registrar,
            IMessageHandler<TMessage, TAsync> handler,
            params IIntercept<TMessage>[] interceptors) => 
                registrar.Register(handler, default, interceptors);
    }
}
