using Chopsticks.Dependencies.Containers;
using Chopsticks.Dependencies.Resolutions;

namespace Chopsticks.Dependencies.Factories
{
    /// <summary>
    /// Manages the construction of dependency resolutions.
    /// </summary>
    public interface IDependencyResolutionFactory
    {
        /// <summary>
        /// Builds a resolution appropriate for the given specification.
        /// </summary>
        /// <param name="specification">The specification that will define the resolution.</param>
        /// <returns>The new resolution.</returns>
        DependencyResolution BuildResolutionFor(DependencySpecification specification);
    }
}
