using Chopsticks.Dependencies.Containers;
using Chopsticks.Dependencies.Factories;

namespace Chopsticks.Dependencies.Contained
{
    /// <summary>
    /// Defines a Unity consumer of dependencies that is itself not a dependency.
    /// </summary>
    public interface IMonoDependent : IUnityDependent<DependencyContainer, 
        UnityContainerService<DependencyContainer, DefaultDependencyContainerFactory, 
            DependencyContainerDefinition>> { }
}
