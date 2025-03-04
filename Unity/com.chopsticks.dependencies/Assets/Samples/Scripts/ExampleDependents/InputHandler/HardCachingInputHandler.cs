using Chopsticks.Dependencies;
using Chopsticks.Samples.ExampleDependencies.ExampleSystem;
using UnityEngine;

namespace Chopsticks.Samples.ExampleDependents.InputHandler
{
    public class HardCachingInputHandler : MonoDependent
    {
        private IExampleSystem _system;


        public void Awake() => 
            GetComponent<Renderer>().material.color = Color.red;


        public override void OnEnable()
        {
            base.OnEnable();

            _system = AssertiveResolve<IExampleSystem>();
        }

        public void OnMouseUp()
        {
            _system.Perform();
        }
    }
}