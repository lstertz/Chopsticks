using System;
using System.Collections.Generic;

namespace Chopsticks.Messages
{
    public abstract class BaseCollectiveMessageHandler<THandler> : 
        ICollectiveMessageHandler<THandler>
        where THandler : IDisposable
    {
        protected List<THandler> Handlers { get; init; } = new();

        public void Dispose()
        {
            foreach (var handler in Handlers)
            {
                handler.Dispose();
            }
            Handlers.Clear();
        }

        public void Deregister(THandler handler)
        {

        }

        public void Register(THandler handler)
        {

        }
    }

}
