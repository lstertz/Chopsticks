using Chopsticks.Messages.Abstractions;

namespace Chopsticks.Messages.TaskBased.Commands
{
    public interface ITaskCommandReceiver<TMessage> :
        IMessageReceiver<TMessage, CollectiveTaskCommandReceiver<TMessage>>
    {
    }
}
