using System.Threading;

namespace Chopsticks.Messages
{
    public interface IBaseAsyncMessageReceiver<TMessage, TAsync>
    {
        TAsync ReceiveAsync(TMessage message,
            CancellationToken token = default, bool runParallel = false);
    }
}
