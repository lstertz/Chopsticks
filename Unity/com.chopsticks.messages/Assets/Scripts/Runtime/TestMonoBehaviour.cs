using UnityEngine;

namespace Chopsticks.Messages
{
    public class TestMonoBehaviour : MonoBehaviour
    {
        public Example Example { get; private set; }

        public void Awake()
        {
            Example = new Example();

            Debug.Log(Example.Test);
        }
    }
}
