using System;
using System.Collections.Generic;

namespace Chopsticks.Dependencies.Resolutions
{
    /// <summary>
    /// Provides resolutions for dependencies.
    /// </summary>
    public interface IDependencyResolutionProvider
    {
        /// <summary>
        /// Specifies whether this provider should inherit all of its parent provider's 
        /// registered dependencies.
        /// </summary>
        bool InheritParentDependencies { get; set; }

        /// <summary>
        /// The parent of this provider.
        /// </summary>
        /// <remarks>
        /// This is null if this provider has no parent.
        /// </remarks>
        IDependencyResolutionProvider? Parent { get; set; }


        /// <summary>
        /// Specifies whether a resolution for the specified contract can be provided.
        /// </summary>
        /// <param name="contract">The contract to be checked for.</param>
        /// <returns>Whether a resolution can be provided for the specified contract.</returns>
        bool CanProvide(Type contract);

        /// <summary>
        /// Provides the first resolution that will resolve the specified contract.
        /// </summary>
        /// <param name="contract">The type of the contract.</param>
        /// <returns>The first resolving resolution, or null if there is no such 
        /// resolution.</returns>
        DependencyResolution? GetResolution(Type contract);

        /// <summary>
        /// Provides all resolutions that will resolve the specified contract.
        /// </summary>
        /// <param name="contract">The type of the contract.</param>
        /// <returns>All resolving resolutions. If there are none, 
        /// the returned collection will be empty.</returns>
        IEnumerable<DependencyResolution> GetResolutions(Type contract);

        /// <summary>
        /// Provides all disposable resolutions known to this provider.
        /// </summary>
        /// <remarks>
        /// This includes all resolutions of a parent provider.
        /// </remarks>
        /// <returns>All disposable resolutions. If there are none, 
        /// the returned collection will be empty.</returns>
        IEnumerable<DependencyResolution> GetResolutionsForDisposal();
    }
}
