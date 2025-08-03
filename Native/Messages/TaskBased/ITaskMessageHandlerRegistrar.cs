using Chopsticks.Messages.Abstractions;

namespace Chopsticks.Messages.TaskBased
{
    public interface ITaskMessageHandlerRegistrar<TMessage> :
        IMessageHandlerRegistrar<TMessage>
    { }
}
