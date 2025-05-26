using System;

namespace Chopsticks.Messages
{
    public interface IRegisteredDataMessageHandler<TMessage, TAsync> : IDisposable
    {
        public TAsync RegisteredHandle(TMessage e);

        // TODO :: Settings.
    }
}
