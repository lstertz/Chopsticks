namespace Chopsticks.Messages.Abstractions
{
    public interface IBaseMessageReceiver<TMessage>
    {
        void Receive(TMessage t);
    }
}
