using System;

namespace Chopsticks.Messages
{
    public interface IRegisteredSignalHandler<TSignal, TAsync, TRegistrar> : 
        IRegisteredDataMessageHandler<TSignal, TAsync>, IDisposable
        where TRegistrar : ICollectiveMessageHandler<IRegisteredSignalHandler<TSignal, TAsync>>, new()
    {
        protected static TRegistrar Registrar { get; private set; } = new();
    }
}
