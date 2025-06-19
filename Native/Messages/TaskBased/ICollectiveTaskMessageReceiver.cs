using System.Threading.Tasks;

namespace Chopsticks.Messages.Abstractions
{
    public interface ICollectiveTaskMessageReceiver<TMessage> :
        ICollectiveMessageReceiver<TMessage, Task<MessageResult>>;
}
