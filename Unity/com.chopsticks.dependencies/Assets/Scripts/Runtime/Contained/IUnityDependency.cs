using Chopsticks.Dependencies.Containers;
using Chopsticks.Dependencies.Resolutions;
using System;

namespace Chopsticks.Dependencies.Contained
{
    /// <summary>
    /// Defines a Unity consumer of dependencies that is itself a dependency.
    /// </summary>
    /// <typeparam name="TNativeContainer">The type of the internal, non-Unity 
    /// dependency container.</typeparam>
    /// <typeparam name="TUnityContainerService">The type of the Unity container service that 
    /// provides Unity-specific services.</typeparam>
    public interface IUnityDependency<TNativeContainer, TUnityContainerService> : 
        IUnityDependent<TNativeContainer, TUnityContainerService>
        where TNativeContainer : IDependencyContainer, IDependencyResolutionProvider, IDisposable
        where TUnityContainerService : IUnityContainerService<TNativeContainer>, new()
    {
        /// <summary>
        /// Performed when the Unity object is disabled.
        /// This performs any deregistration that may be appropriate.
        /// </summary>
        void OnDisable();
    }
}
