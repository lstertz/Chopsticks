using System;

namespace Chopsticks.Messages
{
    public interface IRegisteredSignalHandler<TSignal, TAsync> : IDisposable
    {
        public TAsync RegisteredHandle();

        // TODO :: Settings.
    }
}
