using Chopsticks.Dependencies.Factories;

namespace Chopsticks.Dependencies.Containers
{
    /// <summary>
    /// The definition to define the factory's produced dependency container.
    /// </summary>
    public class DependencyContainerDefinition
    {
        /// <summary>
        /// The resolution factory to be used with the dependency container.
        /// </summary>
        public required IDependencyResolutionFactory ResolutionFactory { get; init; }
    }
}
