using Chopsticks.Messages.Handlers;
using Chopsticks.Messages.Handlers.Multicast;
using Chopsticks.Messages.Registration;
using UnityEngine;

namespace Chopsticks.Messages.Examples
{
    public class HandlerBehaviour : MonoBehaviour, ISyncMessageHandler<TestMessage>
    {
        // Assume this is injected.
        private IMessageHandlerRegistrar<TestMessage> _registrar =
            DefaultMulticastMessageHandler<TestMessage>.Get();


        public void OnEnable()
        {
            _registrar.Register(this);
        }

        public void OnDisable()
        {
            _registrar.Unregister(this);
        }


        void ISyncMessageHandler<TestMessage>.Handle(TestMessage message)
        {
            UnityEngine.Debug.Log($"Received Test Message: {message.Content}.");
        }
    }
}
