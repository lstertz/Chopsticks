using Chopsticks.Dependencies;
using Chopsticks.Samples.ExampleDependencies.ExampleSystem;
using UnityEngine;

namespace Chopsticks.Samples.ExampleDependents.InputHandler
{
    /// <summary>
    /// An example implementation of a <see cref="MonoDependent"/> 
    /// where its dependency is guaranteed (assertively resolved) and cached.
    /// </summary>
    /// <remarks>
    /// This use case is appropriate when the dependencies are not expected to change throughout 
    /// the lifetime of the dependent, but need to be guaranteed and optimized for 
    /// regular use.
    /// </remarks>
    public class HardCachingInputHandler : MonoDependent
    {
        /// <summary>
        /// The cached system.
        /// </summary>
        private IExampleSystem _system;


        /// <summary>
        /// Purely for visual distinction within some scenes.
        /// </summary>
        public void Awake() => 
            GetComponent<Renderer>().material.color = Color.red;

        /// <summary>
        /// Resolves the dependencies of the handler, caching them.
        /// </summary>
        protected override void ResolveDependencies()
        {
            _system = AssertiveResolve<IExampleSystem>();
        }


        /// <summary>
        /// The event trigger upon the object to show use of the dependency.
        /// </summary>
        public void OnMouseUp()
        {
            // As a hard dependency, an exception is thrown when it could not be resolved, 
            // as such, not checking here and throwing additional exceptions may be expected.
            _system.Perform();
        }
    }
}