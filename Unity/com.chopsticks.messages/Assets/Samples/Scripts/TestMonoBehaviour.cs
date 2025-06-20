using Chopsticks.Messages.TaskBased;
using UnityEngine;

namespace Chopsticks.Messages.Examples
{
    public class TestMonoBehaviour : MonoBehaviour
    {
        // Assume this is injected.
        private ITaskMessageReceiver<TestMessage> _testReceiver = 
            ITaskMessageReceiver<TestMessage>.DefaultCollective;


        public void Start()
        {
            var result = _testReceiver.Receive(new()
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
