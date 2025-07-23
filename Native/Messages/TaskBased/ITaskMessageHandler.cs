using Chopsticks.Messages.Abstractions;
using System.Threading.Tasks;

namespace Chopsticks.Messages.TaskBased
{
    public interface ITaskMessageHandler<TMessage> :
        IMessageHandler<TMessage, Task<HandlingResult>>
    {
    }
}
