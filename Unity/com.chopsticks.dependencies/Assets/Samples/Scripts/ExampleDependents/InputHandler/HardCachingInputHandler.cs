using Chopsticks.Dependencies;
using Chopsticks.Samples.ExampleDependencies.System;
using UnityEngine;

namespace Chopsticks.Samples.ExampleDependents.InputHandler
{
    public class HardCachingInputHandler : MonoDependent
    {
        private ISystem _system;


        public void Awake() => 
            GetComponent<Renderer>().material.color = Color.red;


        public override void OnEnable()
        {
            base.OnEnable();

            _system = AssertiveResolve<ISystem>();
        }

        public void OnMouseUp()
        {
            _system.Perform();
        }
    }
}