using Chopsticks.Dependencies;
using Chopsticks.Samples.ExampleDependencies.System;
using UnityEngine;

namespace Chopsticks.Samples.ExampleDependents.InputHandler
{
    public class ConfiguredInputHandler : MonoDependent
    {
        private ISystem System => _system ??= AssertiveResolve<ISystem>();
        private ISystem _system;


        public void Awake() =>
            GetComponent<Renderer>().material.color = Color.white;


        public void OnMouseUp()
        {
            System.Perform();
        }
    }
}