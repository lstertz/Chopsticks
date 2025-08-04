using System;
using System.Runtime.CompilerServices;

namespace Chopsticks.Messages.Handlers.Sources
{
    public class TaskHandlingPromiseSource : 
        BaseHandlingPromiseSource<TaskAwaiter>
    {
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
            VerifyInitialized();
            InnerSource.OnCompleted(continuation);
        }
    }
}