using Chopsticks.Messages.Abstractions;
using System.Threading.Tasks;

namespace Chopsticks.Messages.TaskBased
{
    public interface ITaskMessageReceiverCollective<TMessage> :
        IMessageReceiverCollective<TMessage, Task<MessageResult>>
    { }
}
