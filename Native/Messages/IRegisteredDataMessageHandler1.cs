using System;

namespace Chopsticks.Messages
{
    public interface IRegisteredDataMessageHandler<TMessage, TAsync, TRegistrar> :
        IRegisteredDataMessageHandler<TMessage, TAsync>, IDisposable
        where TRegistrar : ICollectiveMessageHandler<IRegisteredDataMessageHandler<TMessage, TAsync>>, new()
    {
        protected static TRegistrar Registrar { get; private set; } = new();
    }
}
