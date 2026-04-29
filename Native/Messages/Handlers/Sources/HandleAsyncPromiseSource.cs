using System;

namespace Chopsticks.Messages.Handlers.Sources
{
    public class HandleAsyncPromiseSource :
        BaseHandlingPromiseSource<IHandlingPromiseSource>
    {
        /// <inheritdoc/>
        public override bool IsCompleted =>
            InnerSource!.IsCompleted;

        /// <inheritdoc/>
        public override HandlingResult GetResult()
        {
            var result = InnerSource!.GetResult();

            if (result.Status == HandlingStatus.Failure)
                throw new AggregateException(result.Exceptions);

            if (result.Status == HandlingStatus.Cancelled)
                throw new OperationCanceledException();

            return result;
        }

        /// <inheritdoc/>
        public override void OnCompleted(Action continuation) =>
            InnerSource!.OnCompleted(continuation);
    }
}