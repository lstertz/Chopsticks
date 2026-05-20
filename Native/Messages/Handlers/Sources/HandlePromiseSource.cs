using Chopsticks.Messages.Handlers.Sources.Pooling;
using System;

namespace Chopsticks.Messages.Handlers.Sources
{
    /// <summary>
    /// A promise source for exception-oriented handlers.
    /// </summary>
    public class HandlePromiseSource :
        BaseHandlingPromiseSource<IHandlingPromiseSource>
    {
        /// <inheritdoc/>
        public override bool IsCompleted => InnerSource!.IsCompleted;

        /// <inheritdoc/>
        public override HandlingResult GetResult() => InnerSource!.GetResult();

        /// <inheritdoc/>
        public override void OnCompleted(Action continuation)
        {
            InnerSource!.OnCompleted(continuation);
        }
    }
}