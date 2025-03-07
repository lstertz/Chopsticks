using Chopsticks.Dependencies.Containers;
using Chopsticks.Dependencies.Resolutions;
using Chopsticks.Dependencies.Services;
using System;

namespace Chopsticks.Dependencies.Contained
{
    /// <summary>
    /// Defines a Unity consumer of dependencies that is itself not a dependency.
    /// </summary>
    /// <typeparam name="TNativeContainer">The type of the internal, non-Unity 
    /// dependency container.</typeparam>
    /// <typeparam name="TUnityContainerService">The type of the Unity container service that 
    /// provides Unity-specific services.</typeparam>
    public interface IUnityDependent<TNativeContainer, TUnityContainerService> : 
        IUnityContained<TNativeContainer>, 
        IUnityContainerServiceProvider<TNativeContainer, TUnityContainerService>
        where TNativeContainer : IDependencyContainer, IDependencyResolutionProvider, IDisposable
        where TUnityContainerService : IUnityContainerService<TNativeContainer>, new()
    {
        /// <summary>
        /// Performed when the Unity object is enabled.
        /// This ensures the contained has its appropriate container, 
        /// and is registered, if appropriate.
        /// </summary>
        void OnEnable();

        /// <summary>
        /// Performed when the Unity transform hierarchy changes.
        /// This ensures the contained has its appropriate container, 
        /// updating any registrations, if appropriate.
        /// </summary>
        void OnTransformParentChanged();
    }
}
