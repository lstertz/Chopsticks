using Chopsticks.Messages.TaskBased;
using UnityEngine;

namespace Chopsticks.Messages.Examples
{
    public class TestMonoBehaviour : MonoBehaviour
    {
        // Assume this is injected.
        private ISyncTaskMessageHandler<TestMessage> _testHandler =
            IMulticastTaskMessageHandler<TestMessage>.Default;


        public void Start()
        {
            var result = _testHandler.Handle(new()
            {
                Content = "TestMonoBehaviour completed Start!"
            });

            result
                .OnUnprocessed(_ => Debug.LogError(
                    "TestMonoBehaviour :: The message was not received by any handlers."))
                .OnSuccess(_ => Debug.Log(
                    "TestMonoBehaviour :: The message was received successfully!"));
        }
    }
}
