namespace Chopsticks.Messages
{
    public interface IRegisteredMessageReceiver<TCollective>
        where TCollective : new()
    {
        protected static TCollective Collective { get; private set; } = new();
    }
}
