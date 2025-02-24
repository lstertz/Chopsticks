using Chopsticks.Dependencies.Containers;
using Chopsticks.Dependencies.Factories;

namespace Chopsticks.Dependencies.Services
{
    /// <summary>
    /// Provides services for working with <see cref="MonoContainer"/>s, including 
    /// access to a global instance and strategies to work with other containers.
    /// </summary>
    public class MonoContainerService : UnityContainerService<
        DependencyContainer, DefaultDependencyContainerFactory, DependencyContainerDefinition>
    {
    }
}
