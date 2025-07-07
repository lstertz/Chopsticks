using Chopsticks.Messages.Abstractions;
using System.Threading.Tasks;

namespace Chopsticks.Messages.TaskBased
{
    public interface IMulticastTaskMessageHandler<TMessage> :
        ISyncTaskMessageHandler<TMessage>,
        IMulticastMessageHandler<TMessage, Task<HandlingResult>>
    {
        // TODO :: Leverage a builder pattern to create a multicast message handlers.
    }
}
