using Chopsticks.Dependencies;
using UnityEngine;


namespace Chopsticks.Samples.ExampleDependencies.ExampleSystem.Output
{
    /// <summary>
    /// An example class that shows how a MonoBehaviour of the current project can 
    /// be defined, through inheritance of <see cref="MonoDependency"/>, as an injectable 
    /// dependency.
    /// </summary>
    public class OutputSystem : MonoDependency, IExampleSystem
    {
        /// <summary>
        /// The configurable output message for this system.
        /// </summary>
        [SerializeField]
        private string _configuredOutput;


        /// <inheritdoc/>
        public void Perform()
        {
            Debug.Log(_configuredOutput);
        }


        /// <summary>
        /// Performs registrations for this dependency as its contracts, 
        /// for its container.
        /// </summary>
        protected override void PerformRegistration()
        {
            RegisterAs<IExampleSystem>();
        }
    }
}
