using Chopsticks.Dependencies.Contained;
using Chopsticks.Dependencies.Containers;
using Chopsticks.Dependencies.Factories;

namespace Chopsticks.Dependencies
{
    /// <inheritdoc cref="IMonoDependency"/>
    public abstract class MonoDependency : BaseMonoDependency<DependencyContainer,
            UnityContainerService<DependencyContainer, DefaultDependencyContainerFactory,
            DependencyContainerDefinition>>, IMonoDependency { }
}
