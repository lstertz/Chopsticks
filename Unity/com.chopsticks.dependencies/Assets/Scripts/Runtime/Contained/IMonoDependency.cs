using Chopsticks.Dependencies.Containers;
using Chopsticks.Dependencies.Factories;
using Chopsticks.Dependencies.Services;

namespace Chopsticks.Dependencies.Contained
{
    /// <summary>
    /// Defines a Unity consumer of dependencies that is itself a dependency.
    /// </summary>
    public interface IMonoDependency : IUnityDependency<DependencyContainer,
        UnityContainerService<DependencyContainer, DefaultDependencyContainerFactory,
            DependencyContainerDefinition>> { }
}
