using Chopsticks.Dependencies.Containers;
using Chopsticks.Dependencies.Factories;

namespace Chopsticks.Dependencies.Consumers
{
    /// <summary>
    /// Defines a Unity consumer of dependencies that is itself a dependency.
    /// </summary>
    /// <typeparam name="TUnityContainer">The type of the Unity dependency container that 
    /// encapsulates this dependency.</typeparam>
    public interface IMonoDependency<TUnityContainer> : IUnityDependency<DependencyContainer,
        DependencyContainerDefinition, TUnityContainer,
        UnityContainerService<DependencyContainer, DefaultDependencyContainerFactory,
            DependencyContainerDefinition>>
        where TUnityContainer : BaseUnityContainer<DependencyContainer>
    {
    }

    /// <summary>
    /// Defines a Unity consumer of dependencies that is itself a dependency, 
    /// to be contained specifically by <see cref="MonoContainer"/>.
    /// </summary>
    public interface IMonoDependency : IMonoDependency<MonoContainer>
    {
    }
}
