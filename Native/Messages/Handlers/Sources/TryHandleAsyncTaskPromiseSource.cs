using System;
using System.Runtime.CompilerServices;
using Chopsticks.Messages.Handlers.Sources.Pooling;

namespace Chopsticks.Messages.Handlers.Sources
{
    /// <summary>
    /// A promise source for result-oriented async 
    /// <see cref="System.Threading.Tasks.Task"/> handlers.
    /// </summary>
    public class TryHandleAsyncTaskPromiseSource :
        BaseHandlingPromiseSource<TaskAwaiter>
    {
        /// <summary>
        /// The pool of <see cref="TryHandleAsyncTaskPromiseSource"/> instances 
        /// used for reusing promises sources.
        /// </summary>
        public readonly static SourcePool<TryHandleAsyncTaskPromiseSource> Pool = new();


        /// <inheritdoc/>
        public override bool IsCompleted =>
            InnerSource.IsCompleted;


        /// <inheritdoc/>
        public override HandlingResult GetResult()
        {
            VerifyInitialized();

            try
            {
                InnerSource.GetResult();
                return HandlingResult.Success;
            }
            catch (OperationCanceledException)
            {
                return HandlingResult.Cancelled;
            }
            catch (Exception ex)
            {
                return HandlingResult.FromException(ex);
            }
        }

        /// <inheritdoc/>
        public override void OnCompleted(Action continuation)
        {
            try
            {
                VerifyInitialized();
                InnerSource.OnCompleted(continuation);
            }
            finally
            {
                Pool.Return(this);
            }
        }
    }
}