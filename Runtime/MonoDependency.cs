using Chopsticks.Dependencies.Contained;
using Chopsticks.Dependencies.Containers;
using Chopsticks.Dependencies.Services;

namespace Chopsticks.Dependencies
{
    /// <inheritdoc cref="IMonoDependency"/>
    public abstract class MonoDependency : BaseMonoDependency<DependencyContainer, 
        MonoContainerService>, IMonoDependency { }
}
