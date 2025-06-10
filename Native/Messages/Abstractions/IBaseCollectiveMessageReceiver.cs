namespace Chopsticks.Messages.Abstractions
{
    public interface IBaseCollectiveMessageReceiver<TReceiver, TMessage>
        where TReceiver : IBaseMessageReceiver<TMessage>
    {
        void Deregister(TReceiver handler);

        void Register(TReceiver handler, RegistrationSettings settings = default);

        public void Register(TReceiver receiver, params IIntercept<TMessage>[] interceptors);

        public void Register(TReceiver receiver, RegistrationSettings settings,
            params IIntercept<TMessage>[] interceptors);
    }

    public interface IBaseCollectiveAsyncMessageReceiver<TAsyncReceiver, TMessage, TAsync>
        where TAsyncReceiver : IBaseAsyncMessageReceiver<TMessage, TAsync>
    {
        void Deregister(TAsyncReceiver handler);

        void Register(TAsyncReceiver handler, RegistrationSettings settings = default);

        public void Register(TAsyncReceiver receiver, params IIntercept<TMessage>[] interceptors);

        public void Register(TAsyncReceiver receiver, RegistrationSettings settings,
            params IIntercept<TMessage>[] interceptors);
    }
}
