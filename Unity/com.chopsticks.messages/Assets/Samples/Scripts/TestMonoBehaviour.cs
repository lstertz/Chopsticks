using Chopsticks.Messages.TaskBased;
using UnityEngine;

namespace Chopsticks.Messages.Examples
{
    public class TestMonoBehaviour : MonoBehaviour
    {
        // Assume this is injected.
        private ITaskMessageHandler<TestMessage> _testHandler =
            ITaskMulticastMessageHandler<TestMessage>.Default;


        public void Start()
        {
            var result = _testHandler.Handle(new()
            {
                Content = "TestMonoBehaviour completed Start!"
            });

            if (result.CurrentStatus == MessageResult.Status.Success)
                Debug.Log("TestMonoBehaviour :: The message was received successfully!");
            else
                Debug.LogError("TestMonoBehaviour :: The message was not received successfully.");
        }
    }
}
