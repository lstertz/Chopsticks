using System;
using Chopsticks.Messages.Handlers.Sources.Pooling;

namespace Chopsticks.Messages.Handlers.Sources
{
    /// <summary>
    /// A promise source for result-oriented handlers.
    /// </summary>
    public class TryHandlePromiseSource :
        BaseHandlingPromiseSource<HandlingResult>
    {
        /// <summary>
        /// The pool of <see cref="TryHandlePromiseSource"/> instances 
        /// used for reusing promises sources.
        /// </summary>
        public readonly static SourcePool<TryHandlePromiseSource> Pool = new();


        /// <inheritdoc/>
        public override bool IsCompleted => true;


        /// <inheritdoc/>
        public override HandlingResult GetResult() => InnerSource;

        /// <inheritdoc/>
        public override void OnCompleted(Action continuation)
        {
            try
            {
                continuation();
            }
            finally
            {
                Pool.Return(this);
            }
        }
    }
}