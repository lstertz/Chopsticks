using System.Threading;

namespace Chopsticks.Messages.Abstractions
{
    public interface IRegisteredMessageReceiver<TMessage, TAsync>
    {
        TAsync Receive(TMessage message, CancellationToken token = default);
    }

    public interface IRegisteredMessageReceiver<TMessage, TAsync, TCollective> :
        IRegisteredMessageReceiver<TMessage, TAsync>
        where TCollective : ICollectiveMessageReceiver<TMessage, TAsync>, new()
    {
        public static TCollective Collective { get; private set; } = new();
    }
}
