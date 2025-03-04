using Chopsticks.Dependencies;
using Chopsticks.Samples.ExampleDependencies.ExampleSystem;
using UnityEngine;

namespace Chopsticks.Samples.ExampleDependents.InputHandler
{
    public class HardLazyInputHandler : MonoDependent
    {
        private IExampleSystem System => AssertiveResolve<IExampleSystem>();


        public void Awake() =>
            GetComponent<Renderer>().material.color = Color.yellow;


        public void OnMouseUp()
        {
            System.Perform();
        }
    }
}