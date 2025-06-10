using Chopsticks.Messages.Abstractions;
using System.Threading.Tasks;

namespace Chopsticks.Messages.TaskBased.Commands
{
    public interface IAsyncTaskCommandReceiver<TMessage> :
        IAsyncMessageReceiver<TMessage, Task, CollectiveTaskCommandReceiver<TMessage>>
    {
    }
}
