namespace Chopsticks.Messages.Abstractions
{
    public static class ICollectiveMessageReceiverExtensions
    {
        public static bool Register<TMessage, TAsync>(
            this ICollectiveMessageReceiver<TMessage, TAsync> collective,
            IRegisteredMessageReceiver<TMessage, TAsync> receiver,
            RegistrationSettings settings = default) => 
                collective.Register(receiver, settings);

        public static bool Register<TMessage, TAsync>(
            this ICollectiveMessageReceiver<TMessage, TAsync> collective,
            IRegisteredMessageReceiver<TMessage, TAsync> receiver,
            params IIntercept<TMessage>[] interceptors) => 
                collective.Register(receiver, default, interceptors);
    }
}
