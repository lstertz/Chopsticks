using Chopsticks.Dependencies.Contained;
using Chopsticks.Dependencies.Containers;
using Chopsticks.Dependencies.Services;

namespace Chopsticks.Dependencies
{
    /// <inheritdoc cref="IMonoDependencyWrapper"/>
    public abstract class MonoDependencyWrapper : BaseMonoDependencyWrapper<DependencyContainer,
        MonoContainerService>, IMonoDependencyWrapper
    { }
}
