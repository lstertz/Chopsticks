using Chopsticks.Messages.Handlers;
using Chopsticks.Messages.Handlers.Multicast;
using Chopsticks.Messages.Registration;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Chopsticks.Messages.Examples
{
    public class AsyncHandlerBehaviour : MonoBehaviour, ITaskMessageHandler<TestMessage>
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


        async Task ITaskMessageHandler<TestMessage>.HandleAsync(
            TestMessage message, CancellationToken token)
        {
            await Task.Delay(2000);

            UnityEngine.Debug.Log($"Received Test Message Asynchronously: {message.Content}.");
        }
    }
}
