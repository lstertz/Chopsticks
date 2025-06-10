using System.Threading;
using System.Threading.Tasks;

namespace Chopsticks.Messages.TaskBased.Commands
{
    public class CollectiveTaskCommandReceiver<TCommand> :
        CollectiveTaskMessageReceiver<TCommand>,
        ITaskCommandReceiver<TCommand>,
        IAsyncTaskCommandReceiver<TCommand>
    {
        // TODO :: Support command-specific collective settings.

        public override void Receive(TCommand e)
        {
            // TODO :: Apply command-specific constraints to the handling.
            base.Receive(e);
        }

        public override async Task ReceiveAsync(TCommand e,
            CancellationToken token = default, bool runParallel = false)
        {
            // TODO :: Apply command-specific constraints to the handling.
            await base.ReceiveAsync(e, token, runParallel);
        }
    }
}
