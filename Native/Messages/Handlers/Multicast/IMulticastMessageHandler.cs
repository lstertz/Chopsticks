using Chopsticks.Messages.Abstractions;

namespace Chopsticks.Messages
{
    public interface IMulticastMessageHandler<TMessage> : 
        IMessageHandler<TMessage>
    {
    }
}
