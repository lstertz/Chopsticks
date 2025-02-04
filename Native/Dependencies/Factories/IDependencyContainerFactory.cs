using Chopsticks.Dependencies.Containers;

namespace Chopsticks.Dependencies.Factories
{
    /// <summary>
    /// Manages the construction of dependency containers.
    /// </summary>
    public interface IDependencyContainerFactory<TDependencyContainer, TContainerDefinition>
        where TDependencyContainer : IDependencyContainer
    {
        /// <summary>
        /// Builds a new container, per the optional container definition.
        /// </summary>
        /// <param name="definition">The optional definition that 
        /// defines the container.</param>
        TDependencyContainer BuildContainer(TContainerDefinition? definition = default);
    }
}
