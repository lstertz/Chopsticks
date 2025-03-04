using UnityEngine;

namespace Chopsticks.Samples.ExampleDependencies.ExampleSystem.Native
{
    public class NativeSystem : IExampleSystem
    {
        public void Perform()
        {
            Debug.Log("The native system is performing its functionality.");
        }
    }
}