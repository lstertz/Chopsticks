using Chopsticks.Dependencies;

namespace Chopsticks.Samples.ExampleDependencies.ExampleSystem.Native
{
    /// <summary>
    /// An example wrapper of a native system, to enable registration of 
    /// a system that exists outside of Unity.
    /// </summary>
    public class NativeSystemWrapper : MonoDependencyWrapper
    {
        private readonly NativeSystem _system = new();


        /// <summary>
        /// Register the native system for the wrapper's container.
        /// </summary>
        protected override void PerformRegistration()
        {
            Register<IExampleSystem>(_system);
        }

        /// <summary>
        /// Optionally implement to set up any dependencies that the native system may have.
        /// </summary>
        /// <remarks>
        /// Alternatively, this could be done in the constructor of the native system by 
        /// providing a factory that builds the native system with dependencies resolved 
        /// from the container.
        /// </remarks>
        protected override void ResolveDependencies()
        {
        }
    }
}