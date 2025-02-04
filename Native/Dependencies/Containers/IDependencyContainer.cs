using System;
using System.Collections.Generic;

namespace Chopsticks.Dependencies.Containers
{
    /// <summary>
    /// Manages the containment of dependencies, 
    /// providing localized resolution while accounting for dependency scope
    /// and accessibility.
    /// </summary>
    public interface IDependencyContainer
    {
        /// <summary>
        /// Deregisters the dependency identified by the given registration.
        /// </summary>
        /// <param name="registration">The registration that identifies the 
        /// dependency to be deregistered.</param>
        /// <returns>This container, to chain additional manipulations.</returns>
        IDependencyContainer Deregister(DependencyRegistration registration);

        /// <summary>
        /// Registers the dependency defined by the given specification.
        /// </summary>
        /// <param name="specification">The specification that defines the 
        /// dependency to be registered.</param>
        /// <param name="registration">The registration that can identify the dependency for 
        /// deregistration by <see cref="Deregister(DependencyRegistration)"/></param>
        /// <returns>This container, to chain additional manipulations.</returns>
        IDependencyContainer Register(DependencySpecification specification, 
            out DependencyRegistration registration);


        /// <summary>
        /// Resolves the dependency of the specified type with the 
        /// first registered implementation.
        /// </summary>
        /// <param name="contract">The type of the contract to be resolved.</param>
        /// <param name="implementation">The resolving dependency implementation, or null 
        /// if it could not be resolved.</param>
        /// <returns>Whether the dependency was successfully resolved.</returns>
        bool Resolve(Type contract, out object? implementation);


        /// <summary>
        /// Resolves the dependency of the specified type with all registered implementations.
        /// </summary>
        /// <param name="contract">The type of the contract to be resolved.</param>
        /// <returns>The collection of all resolving implementations..</returns>
        IEnumerable<object> ResolveAll(Type contract);
    }
}
