using Chopsticks.Dependencies;
using Chopsticks.Samples.ExampleDependencies.ExampleSystem;
using UnityEngine;

namespace Chopsticks.Samples.ExampleDependents.InputHandler
{
    /// <summary>
    /// An example implementation of a <see cref="MonoDependent"/> 
    /// where its dependency is guaranteed (assertively resolved) but is also re-retrieved 
    /// whenever it is needed.
    /// </summary>
    /// <remarks>
    /// This use case is appropriate when the dependencies might change throughout 
    /// the lifetime of the dependent, but still need to be guaranteed as available.
    /// </remarks>
    public class HardLazyInputHandler : MonoDependent
    {
        /// <summary>
        /// A syntactic wrapper for retrieving the dependency.
        /// </summary>
        private IExampleSystem System => AssertiveResolve<IExampleSystem>();


        /// <summary>
        /// Purely for visual distinction within some scenes.
        /// </summary>
        public void Awake() =>
            GetComponent<Renderer>().material.color = Color.yellow;


        /// <summary>
        /// The event trigger upon the object to show use of the dependency.
        /// </summary>
        public void OnMouseUp()
        {
            System.Perform();
        }
    }
}