using System.Threading.Tasks;

namespace Chopsticks.Messages.Abstractions
{
    public interface ITaskMessageReceiverCollective<TMessage> :
        IMessageReceiverCollective<TMessage, Task<MessageResult>>
    { }
}
