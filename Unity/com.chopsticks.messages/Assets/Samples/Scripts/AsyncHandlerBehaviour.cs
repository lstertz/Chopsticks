using Chopsticks.Messages.Abstractions;
using Chopsticks.Messages.TaskBased;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Chopsticks.Messages.Examples
{
    public class AsyncHandlerBehaviour : MonoBehaviour, ITaskMessageHandler<TestMessage>
    {
        // Assume this is injected.
        private ITaskMessageHandlerRegistrar<TestMessage> _registrar =
            IMulticastTaskMessageHandler<TestMessage>.Default;


        public void OnEnable()
        {
            _registrar.Register(this);
        }

        public void OnDisable()
        {
            _registrar.Unregister(this);
        }


        async Task<MessageResult> ITaskMessageHandler<TestMessage>.HandleAsync(
            TestMessage message, CancellationToken token)
        {
            await Task.Delay(2000);

            UnityEngine.Debug.Log($"Received Test Message Asynchronously: {message.Content}.");

            return MessageResult.Success;
        }
    }
}
