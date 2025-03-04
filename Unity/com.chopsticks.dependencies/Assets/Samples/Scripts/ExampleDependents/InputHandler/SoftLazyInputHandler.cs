using Chopsticks.Dependencies;
using Chopsticks.Samples.ExampleDependencies.ExampleSystem;
using UnityEngine;

namespace Chopsticks.Samples.ExampleDependents.InputHandler
{
    /// <summary>
    /// An example implementation of a <see cref="MonoDependent"/> 
    /// where its dependency is attempted to be retrieved upon every use, 
    /// with accommodations for if the dependency isn't available.
    /// </summary>
    /// <remarks>
    /// This use case is appropriate when the dependencies might change throughout 
    /// the lifetime of the dependent and are not a hard dependency (not absolutely 
    /// required to be operable).
    /// </remarks>
    public class SoftLazyInputHandler : MonoDependent
    {
        /// <summary>
        /// Wrapper property for retrieving the dependency.
        /// </summary>
        private IExampleSystem System
        {
            get
            {
                if (Resolve(out IExampleSystem system))
                    return system;

                Debug.LogWarning($"{nameof(SoftLazyInputHandler)} could not " +
                    $"resolve its {nameof(IExampleSystem)} dependency. Dependent features " +
                    $"will be disabled.");

                return null;
            }

        }


        /// <summary>
        /// Purely for visual distinction within some scenes.
        /// </summary>
        public void Awake() =>
            GetComponent<Renderer>().material.color = Color.green;


        /// <summary>
        /// The event trigger upon the object to show use of the dependency.
        /// </summary>
        public void OnMouseUp()
        {
            System?.Perform();
        }
    }
}