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

        HandlingPromise IMessageHandler<TMessage, Task<HandlingResult>>.Handle(TMessage message)
        {
            // TODO :: Handle contions to produce an already fulfilled promise.
            Handle(message);
            return default;
        }

        Task<HandlingResult> IMessageHandler<TMessage, Task<HandlingResult>>.HandleAsync(
            TMessage message, CancellationToken token)
        {
            // TODO :: Support offloading onto an actual task (optional async setting).

            // TODO :: Handle conditions to produce the result.
            Handle(message);
            return Task.FromResult(HandlingResult.Success);
        }

        new void Handle(TMessage message);
    }
}
