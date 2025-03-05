using Chopsticks.Dependencies;
using Chopsticks.Samples.ExampleDependencies.ExampleSystem;
using UnityEngine;

namespace Chopsticks.Samples.ExampleDependents.InputHandler
{
    /// <summary>
    /// An example of a basic and simple implementation of a <see cref="MonoDependent"/> 
    /// that is configured with a cached dependency upon the first use of that dependency.
    /// </summary>
    /// <remarks>
    /// This is likely the most standard use case of dependents, where its dependencies should 
    /// be guaranteed, cached for the quickest access, and with the expectation that the 
    /// dependency wouldn't change for the lifetime of the dependent.
    /// </remarks>
    public class ConfiguredInputHandler : MonoDependent
    {
        /// <summary>
        /// The cached system, for use in high-performant scenarios.
        /// </summary>
        private IExampleSystem System => _system ??= AssertiveResolve<IExampleSystem>();
        private IExampleSystem _system;


        /// <summary>
        /// Purely for visual distinction within some scenes.
        /// </summary>
        public void Awake() =>
            GetComponent<Renderer>().material.color = Color.white;


        /// <summary>
        /// The event trigger upon the object to show use of the dependency.
        /// </summary>
        public void OnMouseUp()
        {
            System.Perform();
        }
    }
}