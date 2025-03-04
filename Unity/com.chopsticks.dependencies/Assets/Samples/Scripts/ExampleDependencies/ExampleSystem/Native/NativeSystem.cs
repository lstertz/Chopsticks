using UnityEngine;

namespace Chopsticks.Samples.ExampleDependencies.ExampleSystem.Native
{
    /// <summary>
    /// An example native (non-MonoBehaviour, potentially third-party) implementation 
    /// of the example system.
    /// </summary>
    public class NativeSystem : IExampleSystem
    {
        /// <inheritdoc/>
        public void Perform()
        {
            Debug.Log("The native system is performing its functionality.");
        }
    }
}