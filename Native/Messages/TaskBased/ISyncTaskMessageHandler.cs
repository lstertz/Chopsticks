using Chopsticks.Messages.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Chopsticks.Messages.TaskBased
{
    public interface ISyncTaskMessageHandler<TMessage> :
        ITaskMessageHandler<TMessage>
    {
        private static Task<MessageResult> Success = Task.FromResult(MessageResult.Success);

        Task<MessageResult> ITaskMessageHandler<TMessage>.HandleAsync(TMessage message,
            CancellationToken token)
        {
            try
            {
                var result = Handle(message);
                return Success;
            }
            catch (Exception e)
            {
                // TODO :: Include the exception, maybe try to optimize from making the Task.
                return Task.FromResult(new MessageResult()
                {
                    CurrentStatus = MessageResult.Status.Failure,
                });
            }
        }

        MessageResult Handle(TMessage t);
    }
}
