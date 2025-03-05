using UnityEngine;


namespace Chopsticks.Samples.ExampleDependencies.ExampleSystem.Counting
{
    /// <summary>
    /// An example class standing-in for some unalterable (third-party or Unity native) 
    /// superclass, such that this class cannot be directly made into a 
    /// <see cref="Dependencies.MonoDependency"/>.
    /// </summary>
    public abstract class UnalterableCountingSystemSuperclass : MonoBehaviour, IExampleSystem
    {
        private int _count;

        ///<inheritdoc/>
        public void Perform()
        {
            _count++;
            Debug.Log($"Current performance count: {_count}");
        }
    }
}