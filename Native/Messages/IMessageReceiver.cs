namespace Chopsticks.Messages
{
    public interface IMessageReceiver<TMessage, TCollective> :
        IBaseMessageReceiver<TMessage>,
        IRegisteredMessageReceiver<TCollective>
        where TCollective : IBaseCollectiveMessageReceiver<IBaseMessageReceiver<TMessage>, TMessage>, 
            new()
    {
        public static TCollective All => Collective;
    }
}
