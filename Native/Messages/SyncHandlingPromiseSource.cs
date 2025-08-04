using System;

namespace Chopsticks.Messages
{
    public class SyncHandlingPromiseSource :
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