using Chopsticks.Dependencies.Containers;
using Chopsticks.Dependencies.Services;

namespace Chopsticks.Dependencies.Contained
{
    /// <summary>
    /// Provides extensions to Unity monodependency implementations.
    /// </summary>
    public static class IMonoDependencyExtensions
    {
        /// <summary>
        /// Registers this dependency as the specified contract for its container.
        /// </summary>
        /// <typeparam name="TContract">The contract type that the dependency 
        /// is being registered as.</typeparam>
        /// <param name="dependency">This dependency that is being registered.</param>
        public static DependencyRegistration RegisterAs<TContract>(
            this IUnityDependency<DependencyContainer, MonoContainerService> dependency) => 
            dependency.RegisterAs<DependencyContainer, MonoContainerService, TContract>();
    }
}
