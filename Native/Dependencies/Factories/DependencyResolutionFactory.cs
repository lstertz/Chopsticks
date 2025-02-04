using Chopsticks.Dependencies.Containers;
using Chopsticks.Dependencies.Resolutions;
using System;

namespace Chopsticks.Dependencies.Factories
{
    /// <inheritdoc cref="IDependencyResolutionFactory"/>
    public class DependencyResolutionFactory : IDependencyResolutionFactory
    {
        /// <inheritdoc/>
        public DependencyResolution BuildResolutionFor(DependencySpecification specification) =>
            specification.Lifetime switch
            {
                DependencyLifetime.Contained => new ContainedResolution(
                    specification.Contract, specification.ImplementationFactory),
                DependencyLifetime.Singleton => new SingletonResolution(
                    specification.Contract, specification.ImplementationFactory),
                DependencyLifetime.Transient => new TransientResolution(
                    specification.Contract, specification.ImplementationFactory),
                _ => throw new NotImplementedException($"The lifetime of " +
                    $"{specification.Lifetime} has no matching resolution for this factory.")
            };
    }
}
