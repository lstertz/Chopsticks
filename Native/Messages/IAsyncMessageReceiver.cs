namespace Chopsticks.Messages
{
    public interface IAsyncMessageReceiver<TMessage, TAsync, TCollective> : 
        IBaseAsyncMessageReceiver<TMessage, TAsync>,
        IRegisteredMessageReceiver<TCollective>
        where TCollective : IBaseCollectiveAsyncMessageReceiver<IBaseAsyncMessageReceiver<TMessage, TAsync>, 
            TMessage, TAsync>, new()
    {
        public static TCollective All => Collective;
    }
}
