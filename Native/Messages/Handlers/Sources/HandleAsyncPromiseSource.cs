using System;
using Chopsticks.Messages.Handlers.Sources.Pooling;

namespace Chopsticks.Messages.Handlers.Sources
{
    /// <summary>
    /// A promise source for exception-oriented async handlers.
    /// </summary>
    public class HandleAsyncPromiseSource :
        BaseHandlingPromiseSource<IHandlingPromiseSource>
    {
        /// <summary>
        /// The pool of <see cref="HandleAsyncPromiseSource"/> instances 
        /// used for reusing promises sources.
        /// </summary>
        public readonly static SourcePool<HandleAsyncPromiseSource> Pool = new();


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
        public override void OnCompleted(Action continuation)
        {
            try
            {
                InnerSource!.OnCompleted(continuation);
            }
            finally
            {
                Pool.Return(this);
            }
        }
    }
}