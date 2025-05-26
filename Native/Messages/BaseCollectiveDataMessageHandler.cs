namespace Chopsticks.Messages
{
    public abstract class BaseCollectiveDataMessageHandler<TEvent, TAsync> :
        BaseCollectiveMessageHandler<IRegisteredDataMessageHandler<TEvent, TAsync>>,
        IRegisteredDataMessageHandler<TEvent, TAsync>
    {

        public abstract TAsync RegisteredHandle(TEvent e);
    }

}
