namespace Chopsticks.Messages.Abstractions
{
    public static class IMessageReceiverCollectiveExtensions
    {
        public static bool Register<TMessage, TAsync>(
            this IMessageReceiverCollective<TMessage, TAsync> collective,
            IMessageReceiver<TMessage, TAsync> receiver,
            RegistrationSettings settings = default) => 
                collective.Register(receiver, settings);

        public static bool Register<TMessage, TAsync>(
            this IMessageReceiverCollective<TMessage, TAsync> collective,
            IMessageReceiver<TMessage, TAsync> receiver,
            params IIntercept<TMessage>[] interceptors) => 
                collective.Register(receiver, default, interceptors);
    }
}
