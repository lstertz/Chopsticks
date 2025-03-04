using UnityEngine;


namespace Chopsticks.Samples.ExampleDependencies.System.Counting
{
    public abstract class UnalterableCountingSystemSuperclass : MonoBehaviour, ISystem
    {
        private int _count;

        public void Perform()
        {
            _count++;
            Debug.Log($"Current performance count: {_count}");
        }
    }
}