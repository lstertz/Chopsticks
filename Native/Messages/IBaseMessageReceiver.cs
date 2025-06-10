namespace Chopsticks.Messages
{
    public interface IBaseMessageReceiver<TMessage>
    {
        void Receive(TMessage t);
    }
}
