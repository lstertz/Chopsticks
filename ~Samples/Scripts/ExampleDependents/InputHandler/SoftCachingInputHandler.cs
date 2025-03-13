using Chopsticks.Dependencies;
using Chopsticks.Samples.ExampleDependencies.ExampleSystem;
using UnityEngine;

namespace Chopsticks.Samples.ExampleDependents.InputHandler
{
    /// <summary>
    /// An example implementation of a <see cref="MonoDependent"/> 
    /// where its dependency is not guaranteed when first attempted to be retrieved, 
    /// and any retrieved dependency is cached.
    /// </summary>
    /// <remarks>
    /// This use case is appropriate when the dependencies are not expected to change throughout 
    /// the lifetime of the dependent and are not a hard dependency (not absolutely 
    /// required to be operable).
    /// </remarks>
    public class SoftCachingInputHandler : MonoDependent
    {
        /// <summary>
        /// The cached system.
        /// </summary>
        private IExampleSystem _system;


        /// <summary>
        /// Purely for visual distinction within some scenes.
        /// </summary>
        public void Awake() =>
            GetComponent<Renderer>().material.color = Color.blue;

        /// <summary>
        /// Resolves the dependencies of the handler, caching them and logging a warning if 
        /// a dependency could not be resolved.
        /// </summary>
        protected override void ResolveDependencies()
        {
            if (!Resolve(out _system))
                Debug.LogWarning($"{nameof(SoftCachingInputHandler)} could not " +
                    $"resolve its {nameof(IExampleSystem)} dependency. Dependent features " +
                    $"will be disabled.");
        }


        /// <summary>
        /// The event trigger upon the object to show use of the dependency.
        /// </summary>
        public void OnMouseUp()
        {
            _system?.Perform();
        }
    }
}