using System.Threading;

namespace Chopsticks.Messages.Abstractions
{
    public interface IBaseAsyncMessageReceiver<TMessage, TAsync>
    {
        TAsync ReceiveAsync(TMessage message,
            CancellationToken token = default, bool runParallel = false);
    }
}
