using Chopsticks.Dependencies;
using Chopsticks.Samples.ExampleDependencies.ExampleSystem;
using UnityEngine;

namespace Chopsticks.Samples.ExampleDependents.InputHandler
{
    public class SoftCachingInputHandler : MonoDependent
    {
        private IExampleSystem _system;


        public void Awake() =>
            GetComponent<Renderer>().material.color = Color.blue;


        public override void OnEnable()
        {
            base.OnEnable();

            if (!Resolve(out _system))
                Debug.LogWarning($"{nameof(SoftCachingInputHandler)} could not " +
                    $"resolve its {nameof(IExampleSystem)} dependency. Dependent features " +
                    $"will be disabled.");
        }

        public void OnMouseUp()
        {
            _system?.Perform();
        }
    }
}