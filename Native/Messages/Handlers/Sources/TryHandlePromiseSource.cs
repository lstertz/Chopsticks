using System;

namespace Chopsticks.Messages.Handlers.Sources
{
    public class TryHandlePromiseSource :
        BaseHandlingPromiseSource<HandlingResult>
    {
        /// <inheritdoc/>
        public override bool IsCompleted => true;


        /// <inheritdoc/>
        public override HandlingResult GetResult() => InnerSource;

        /// <inheritdoc/>
        public override void OnCompleted(Action continuation) => 
            continuation();
    }
}