using Chopsticks.Messages.Abstractions;
using System.Threading.Tasks;

namespace Chopsticks.Messages.TaskBased
{
    public interface ITaskMulticastMessageHandler<TMessage> :
        ITaskMessageHandler<TMessage>,
        IMulticastMessageHandler<TMessage, Task<MessageResult>>
    {
        // TODO :: Leverage a builder pattern to create a multicast message handlers.
        public static TaskMulticastMessageHandler<TMessage> Default { get; } =
            new TaskMulticastMessageHandler<TMessage>();
    }
}
