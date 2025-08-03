namespace Chopsticks.Messages.Abstractions
{
    public static class IMessageHandlerRegistrarExtensions
    {
        public static bool Register<TMessage, TAsync>(
            this IMessageHandlerRegistrar<TMessage> registrar,
            IMessageHandler<TMessage> handler,
            params IIntercept<TMessage>[] interceptors) => 
                registrar.Register(handler, default, interceptors);
    }
}
