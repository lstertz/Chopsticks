using Chopsticks.Dependencies.Containers;

namespace Chopsticks.Dependencies.Factories
{
    /// <summary>
    /// Provides a default strategy to manage the construction of dependency containers.
    /// </summary>
    public class DefaultDependencyContainerFactory : 
        IDependencyContainerFactory<DependencyContainer, DependencyContainerDefinition>
    {
        /// <inheritdoc/>
        public DependencyContainer BuildContainer(
            DependencyContainerDefinition? definition = default)
        {
            if (definition is null)
                return new();
            return new(definition.ResolutionFactory);
        }
    }
}
