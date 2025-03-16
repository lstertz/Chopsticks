using Chopsticks.Dependencies.Containers;
using Chopsticks.Dependencies.Resolutions;
using Chopsticks.Dependencies.Services;
using System;

namespace Chopsticks.Dependencies.Contained
{
    /// <summary>
    /// Provides extensions to Unity dependency wrappers.
    /// </summary>
    public static class IUnityDependencyWrapperExtensions
    {
        /// <summary>
        /// Deregisters all of this dependency wrapper's registrations with its container.
        /// </summary>
        /// <typeparam name="TNativeContainer">The type of the internal, non-Unity 
        /// dependency container.</typeparam>
        /// <typeparam name="TUnityContainerService">The type of the Unity container service that 
        /// provides Unity-specific services.</typeparam>
        /// <param name="dependency">This dependency wrapper that is having its registrations 
        /// deregistered.</param>
        public static void DeregisterAll<TNativeContainer, TUnityContainerService>(
            this IUnityDependencyWrapper<TNativeContainer, TUnityContainerService> dependency)
            where TNativeContainer : IDependencyContainer, IDependencyResolutionProvider, IDisposable
            where TUnityContainerService : IUnityContainerService<TNativeContainer>, new()
        {
            foreach (var registration in dependency.Registrations)
                dependency.Container.Deregister(registration);
            dependency.Registrations.Clear();
        }

        // TODO :: Register implementations.
    }
}
