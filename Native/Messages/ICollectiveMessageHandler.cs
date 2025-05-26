using System;

namespace Chopsticks.Messages
{
    public interface ICollectiveMessageHandler<THandler> : IDisposable
    {
        void Deregister(THandler handler);

        void Register(THandler handler);
    }
}
