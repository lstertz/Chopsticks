using Chopsticks.Dependencies.Resolutions;
using System;

namespace Chopsticks.Dependencies.Containers
{
    /// <summary>
    /// Defines a container to be used through Unity.
    /// </summary>
    /// <typeparam name="TNativeContainer">The type of the internal, non-Unity container 
    /// that manages the dependencies.</typeparam>
    public interface IUnityContainer<TNativeContainer>
        where TNativeContainer : IDependencyContainer, IDependencyResolutionProvider, IDisposable
    {
        /// <summary>
        /// The internal, non-Unity container that manages the dependencies of 
        /// this Unity Container.
        /// </summary>
        TNativeContainer NativeContainer { get; }
    }
}
