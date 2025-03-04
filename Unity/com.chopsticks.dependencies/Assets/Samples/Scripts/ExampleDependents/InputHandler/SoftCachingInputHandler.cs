using Chopsticks.Dependencies;
using Chopsticks.Samples.ExampleDependencies.System;
using UnityEngine;

namespace Chopsticks.Samples.ExampleDependents.InputHandler
{
    public class SoftCachingInputHandler : MonoDependent
    {
        private ISystem _system;


        public void Awake() =>
            GetComponent<Renderer>().material.color = Color.blue;


        public override void OnEnable()
        {
            base.OnEnable();

            if (!Resolve(out _system))
                Debug.LogWarning($"{nameof(SoftCachingInputHandler)} could not " +
                    $"resolve its {nameof(ISystem)} dependency. Dependent features " +
                    $"will be disabled.");
        }

        public void OnMouseUp()
        {
            _system?.Perform();
        }
    }
}