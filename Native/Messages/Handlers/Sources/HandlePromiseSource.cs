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
        /// <summary>
        /// The pool of <see cref="HandlePromiseSource"/> instances 
        /// used for reusing promises sources.
        /// </summary>
        public readonly static SourcePool<HandlePromiseSource> Pool = new();


        /// <inheritdoc/>
        public override bool IsCompleted =>
            InnerSource!.IsCompleted;

        /// <inheritdoc/>
        public override HandlingResult GetResult()
        {
            var result = InnerSource!.GetResult();
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