using UnityEngine;


namespace Chopsticks.Samples.ExampleDependencies.ExampleSystem.Counting
{
    public abstract class UnalterableCountingSystemSuperclass : MonoBehaviour, IExampleSystem
    {
        private int _count;

        public void Perform()
        {
            _count++;
            Debug.Log($"Current performance count: {_count}");
        }
    }
}