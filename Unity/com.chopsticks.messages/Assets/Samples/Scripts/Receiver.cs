using Chopsticks.Messages.Abstractions;
using Chopsticks.Messages.TaskBased;
using UnityEngine;

namespace Chopsticks.Messages.Examples
{
    public class ReceiverBehaviour : MonoBehaviour, ITaskMessageReceiver<TestMessage>
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


        MessageResult ITaskMessageReceiver<TestMessage>.Receive(TestMessage message)
        {
            UnityEngine.Debug.Log($"Received Test Message: {message.Content}.");
            return MessageResult.Success;
        }
    }
}
