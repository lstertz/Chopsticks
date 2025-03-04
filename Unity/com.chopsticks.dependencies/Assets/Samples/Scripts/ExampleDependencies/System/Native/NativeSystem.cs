using UnityEngine;

namespace Chopsticks.Samples.ExampleDependencies.System.Native
{
    public class NativeSystem : ISystem
    {
        public void Perform()
        {
            Debug.Log("The native system is performing its functionality.");
        }
    }
}