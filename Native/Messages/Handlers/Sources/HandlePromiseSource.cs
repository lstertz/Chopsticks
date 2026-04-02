using System;

namespace Chopsticks.Messages.Handlers.Sources
{
    public class HandlePromiseSource :
        BaseHandlingPromiseSource<IHandlingPromiseSource>
    {
        /// <inheritdoc/>
        public override bool IsCompleted => 
            InnerSource!.IsCompleted;

        /// <inheritdoc/>
        public override HandlingResult GetResult()
        {
            var result = InnerSource!.GetResult();
            result.ThrowIfFailed();

            return result;
        }

        /// <inheritdoc/>
        public override void OnCompleted(Action continuation) =>
            InnerSource!.OnCompleted(continuation);
    }
}