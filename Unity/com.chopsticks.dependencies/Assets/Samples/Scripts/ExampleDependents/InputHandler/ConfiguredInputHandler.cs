using Chopsticks.Dependencies;
using Chopsticks.Samples.ExampleDependencies.ExampleSystem;
using UnityEngine;

namespace Chopsticks.Samples.ExampleDependents.InputHandler
{
    public class ConfiguredInputHandler : MonoDependent
    {
        private IExampleSystem System => _system ??= AssertiveResolve<IExampleSystem>();
        private IExampleSystem _system;


        public void Awake() =>
            GetComponent<Renderer>().material.color = Color.white;


        public void OnMouseUp()
        {
            System.Perform();
        }
    }
}