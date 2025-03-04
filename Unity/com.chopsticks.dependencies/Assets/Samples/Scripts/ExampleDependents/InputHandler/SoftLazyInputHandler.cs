using Chopsticks.Dependencies;
using Chopsticks.Samples.ExampleDependencies.System;
using UnityEngine;

namespace Chopsticks.Samples.ExampleDependents.InputHandler
{
    public class SoftLazyInputHandler : MonoDependent
    {
        private ISystem System
        {
            get
            {
                if (Resolve(out ISystem system))
                    return system;

                UnityEngine.Debug.LogWarning($"{nameof(SoftLazyInputHandler)} could not " +
                    $"resolve its {nameof(ISystem)} dependency. Dependent features " +
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