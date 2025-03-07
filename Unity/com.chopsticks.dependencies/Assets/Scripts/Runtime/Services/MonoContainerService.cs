using Chopsticks.Dependencies.Containers;
using Chopsticks.Dependencies.Factories;

namespace Chopsticks.Dependencies.Services
{
    /// <summary>
    /// Provides services for working with <see cref="MonoContainer"/>s, including 
    /// access to a global instance and strategies to work with other containers.
    /// </summary>
    public class MonoContainerService : UnityContainerService<
        DependencyContainer, DefaultDependencyContainerFactory, DependencyContainerDefinition>,
        IUnityContainerServiceProvider<DependencyContainer, MonoContainerService>
    {
        /// <summary>
        /// The service instance used across all standard <see cref="MonoDependency"/> 
        /// and <see cref="MonoDependent"/> instances to integrate 
        /// with <see cref="MonoContainer"/>s.
        /// </summary>
        public static MonoContainerService Shared =>
            IUnityContainerServiceProvider<DependencyContainer, MonoContainerService>.InternalService;
    }
}
