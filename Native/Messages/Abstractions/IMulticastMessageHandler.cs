namespace Chopsticks.Messages.Abstractions
{

    public interface IMulticastMessageHandler<TMessage> : 
        IMessageHandler<TMessage>
    {
    }
}
