using Chopsticks.Messages.Abstractions;
using System.Threading.Tasks;

namespace Chopsticks.Messages.TaskBased
{
    public interface ITaskMulticastMessageRegistrar<TMessage> :
        IMulticastMessageRegistrar<TMessage, Task<MessageResult>>
    { }
}
