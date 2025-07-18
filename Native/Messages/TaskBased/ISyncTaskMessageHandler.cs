using Chopsticks.Messages.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Chopsticks.Messages.TaskBased
{
    public interface ISyncTaskMessageHandler<TMessage> :
        ITaskMessageHandler<TMessage>
    {
        private static Task<HandlingResult> Success = Task.FromResult(HandlingResult.Success);

        Task<HandlingResult> ITaskMessageHandler<TMessage>.HandleAsync(TMessage message,
            CancellationToken token)
        {
            try
            {
                // TODO :: Support offloading to a task scheduler as an async option.

                Handle(message);

                // The handler is known to be synchronous here, so if it completed without 
                // an exception, a successful result can be returned.
                return Success;
            }
            catch (Exception e)
            {
                return Task.FromResult(HandlingResult.FromException(e));
            }
        }

        HandlingPromise Handle(TMessage t);
    }
}
