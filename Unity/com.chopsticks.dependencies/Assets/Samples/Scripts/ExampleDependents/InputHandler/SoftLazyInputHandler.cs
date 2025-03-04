using Chopsticks.Dependencies;
using Chopsticks.Samples.ExampleDependencies.ExampleSystem;
using UnityEngine;

namespace Chopsticks.Samples.ExampleDependents.InputHandler
{
    public class SoftLazyInputHandler : MonoDependent
    {
        private IExampleSystem System
        {
            get
            {
                if (Resolve(out IExampleSystem system))
                    return system;

                UnityEngine.Debug.LogWarning($"{nameof(SoftLazyInputHandler)} could not " +
                    $"resolve its {nameof(IExampleSystem)} dependency. Dependent features " +
                    $"will be disabled.");

                return null;
            }

        }


        public void Awake() =>
            GetComponent<Renderer>().material.color = Color.green;


        public void OnMouseUp()
        {
            System?.Perform();
        }
    }
}