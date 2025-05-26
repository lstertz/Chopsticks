namespace Chopsticks.Messages
{
    public abstract class BaseSignalHandlerCollective<TEvent, TAsync> : 
        BaseCollectiveMessageHandler<IRegisteredSignalHandler<TEvent, TAsync>>,
        IRegisteredSignalHandler<TEvent, TAsync>
    {
        public abstract TAsync RegisteredHandle();
    }
}
