namespace Chopsticks.Messages.Handlers.Sources.Pooling
{
    public interface ISourcePool
    {
        IHandlingPromiseSource Rent();

        void Return(IHandlingPromiseSource source);
    }
}