using Chopsticks.Messages.Abstractions;
using System.Threading.Tasks;

namespace Chopsticks.Messages.TaskBased
{
    public interface ITaskMessageHandlerRegistrar<TMessage> :
        IMessageHandlerRegistrar<TMessage, Task<MessageResult>>
    { }
}
