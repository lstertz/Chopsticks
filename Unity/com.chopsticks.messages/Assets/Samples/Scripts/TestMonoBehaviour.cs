using Chopsticks.Messages.Handlers;
using Chopsticks.Messages.Handlers.Multicast;
using UnityEngine;

namespace Chopsticks.Messages.Examples
{
    public class TestMonoBehaviour : MonoBehaviour
    {
        // Assume this is injected.
        private IMessageHandler<TestMessage> _testHandler =
            DefaultMulticastMessageHandler<TestMessage>.Get();


        public void Start()
        {
            var result = _testHandler.TryHandle(new()
            {
                Content = "TestMonoBehaviour completed Start!"
            });

            result
                .WhenNotHandled(() => Debug.LogError(
                    "TestMonoBehaviour :: The message was not received by any handlers."))
                .OnSuccess(() =>
                {
                    Debug.Log("TestMonoBehaviour :: The message was received successfully!");
                    _testHandler.Handle(new()
                    {
                        Content = "TestMonoBehaviour completed Start from a success callback!"
                    });
                });

            result = _testHandler.TryHandle(new()
            {
                Content = "TestMonoBehaviour completed Start 2!"
            });

            result
                .WhenNotHandled(() => Debug.LogError(
                    "TestMonoBehaviour :: The message was not received by any handlers 2."))
                .OnSuccess(() => Debug.Log(
                    "TestMonoBehaviour :: The message was received successfully 2!"));
        }
    }
}
