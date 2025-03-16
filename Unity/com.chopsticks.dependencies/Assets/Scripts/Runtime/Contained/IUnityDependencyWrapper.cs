using Chopsticks.Dependencies.Containers;
using Chopsticks.Dependencies.Resolutions;
using Chopsticks.Dependencies.Services;
using System;
using System.Collections.Generic;

namespace Chopsticks.Dependencies.Contained
{
    /// <summary>
    /// Defines a Unity consumer of dependencies that is a wrapper for 
    /// a non-Unity dependency.
    /// </summary>
    /// <typeparam name="TNativeContainer">The type of the internal, non-Unity 
    /// dependency container.</typeparam>
    /// <typeparam name="TUnityContainerService">The type of the Unity container service that 
    /// provides Unity-specific services.</typeparam>
    public interface IUnityDependencyWrapper<TNativeContainer, TUnityContainerService> :
        IUnityDependent<TNativeContainer, TUnityContainerService>
        where TNativeContainer : IDependencyContainer, IDependencyResolutionProvider, IDisposable
        where TUnityContainerService : IUnityContainerService<TNativeContainer>, new()
    {
        /// <summary>
        /// The registrations that represent the wrapped dependency as its various 
        /// registered contracts within its current container.
        /// </summary>
        List<DependencyRegistration> Registrations { get; }

        /// <summary>
        /// Performed when the Unity object is disabled.
        /// This performs any deregistration that may be appropriate.
        /// </summary>
        void OnDisable();
    }
}
