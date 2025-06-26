using Chopsticks.Messages.Abstractions;
using Chopsticks.Messages.TaskBased;
using UnityEngine;

namespace Chopsticks.Messages.Examples
{
    public class HandlerBehaviour : MonoBehaviour, ITaskMessageHandler<TestMessage>
    {
        // Assume this is injected.
        private ITaskMulticastMessageRegistrar<TestMessage> _registrar =
            ITaskMulticastMessageHandler<TestMessage>.Default;


        public void OnEnable()
        {
            _registrar.Register(this);
        }

        public void OnDisable()
        {
            _registrar.Unregister(this);
        }


        MessageResult ITaskMessageHandler<TestMessage>.Handle(TestMessage message)
        {
            UnityEngine.Debug.Log($"Received Test Message: {message.Content}.");
            return MessageResult.Success;
        }
    }
}
