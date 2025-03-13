using Chopsticks.Dependencies.Containers;
using Chopsticks.Dependencies.Resolutions;
using Chopsticks.Dependencies.Services;
using System;
using UnityEngine;

namespace Chopsticks.Dependencies.Contained
{
    /// <summary>
    /// Provides extensions to Unity dependencies.
    /// </summary>
    public static class IUnityDependencyExtensions
    {
        /// <summary>
        /// Deregisters all of this dependency's registrations with its container.
        /// </summary>
        /// <typeparam name="TNativeContainer">The type of the internal, non-Unity 
        /// dependency container.</typeparam>
        /// <typeparam name="TUnityContainerService">The type of the Unity container service that 
        /// provides Unity-specific services.</typeparam>
        /// <param name="dependency">This dependency that is having its registrations 
        /// deregistered.</param>
        public static void DeregisterAll<TNativeContainer, TUnityContainerService>(
            this IUnityDependency<TNativeContainer, TUnityContainerService> dependency)
            where TNativeContainer : IDependencyContainer, IDependencyResolutionProvider, IDisposable
            where TUnityContainerService : IUnityContainerService<TNativeContainer>, new()
        {
            foreach (var registration in dependency.Registrations)
                dependency.Container.Deregister(registration);
            dependency.Registrations.Clear();
        }

        /// <summary>
        /// Registers this dependency as the specified contract for its container.
        /// </summary>
        /// <typeparam name="TNativeContainer">The type of the internal, non-Unity 
        /// dependency container.</typeparam>
        /// <typeparam name="TUnityContainerService">The type of the Unity container service that 
        /// provides Unity-specific services.</typeparam>
        /// <typeparam name="TContract">The contract type that the dependency 
        /// is being registered as.</typeparam>
        /// <param name="dependency">This dependency that is being registered.</param>
        public static DependencyRegistration RegisterAs<TNativeContainer, 
                TUnityContainerService, TContract>(
            this IUnityDependency<TNativeContainer, TUnityContainerService> dependency)
            where TNativeContainer : IDependencyContainer, IDependencyResolutionProvider, IDisposable
            where TUnityContainerService : IUnityContainerService<TNativeContainer>, new()
        {
            if (dependency is not TContract asContract)
            {
                Debug.LogError($"Failed to register a dependency of type " +
                    $"{dependency.GetType().FullName} as a contract of type {nameof(TContract)}.");
                return null;
            }

            dependency.Container.Register(asContract, out var registration);
            dependency.Registrations.Add(registration);
            return registration;
        }
    }
}
