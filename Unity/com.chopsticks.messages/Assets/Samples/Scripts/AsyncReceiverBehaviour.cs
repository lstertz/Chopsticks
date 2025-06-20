using Chopsticks.Messages.Abstractions;
using Chopsticks.Messages.TaskBased;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Chopsticks.Messages.Examples
{
    public class AsyncReceiverBehaviour : MonoBehaviour, ITaskMessageAsyncReceiver<TestMessage>
    {
        // Assume this is injected.
        private ITaskMessageReceiverCollective<TestMessage> _collective =
            ITaskMessageReceiver<TestMessage>.DefaultCollective;


        public void OnEnable()
        {
            _collective.Register(this);
        }

        public void OnDisable()
        {
            _collective.Unregister(this);
        }


        async Task<MessageResult> ITaskMessageAsyncReceiver<TestMessage>.ReceiveAsync(
            TestMessage message, CancellationToken token)
        {
            await Task.Delay(2000);

            UnityEngine.Debug.Log($"Received Test Message Asynchronously: {message.Content}.");

            return MessageResult.Success;
        }
    }
}
