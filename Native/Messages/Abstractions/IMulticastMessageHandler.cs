namespace Chopsticks.Messages.Abstractions
{

    public interface IMulticastMessageHandler<TMessage, TAsync> : 
        IMessageHandler<TMessage, TAsync>
    {
    }
}
