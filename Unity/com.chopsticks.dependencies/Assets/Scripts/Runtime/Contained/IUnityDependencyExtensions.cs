using Chopsticks.Dependencies.Containers;
using Chopsticks.Dependencies.Resolutions;
using Chopsticks.Dependencies.Services;
using Examples;
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
        /// Sets the container of the dependency based on its current transform hierarchy.
        /// </summary>
        /// <remarks>
        /// Usually called as part of 
        /// <see cref="IUnityDependent{TNativeContainer, TUnityContainerService}.OnEnable"/>.
        /// </remarks>
        /// <typeparam name="TNativeContainer">The type of the internal, non-Unity 
        /// dependency container.</typeparam>
        /// <typeparam name="TUnityContainerService">The type of the Unity container service that 
        /// provides Unity-specific services.</typeparam>
        /// <param name="dependency">This dependency that is having its container set.</param>
        /// <param name="unityContained">The MonoBehaviour of this dependency.</param>
        /// <param name="overrideContainer">The container that may be used as an override 
        /// when setting the container.</param>
        /// <param name="onRegistration">The method called when the container has been set, 
        /// to initiate registration of the dependency as its contracts.</param>
        public static void SetContainer<TNativeContainer, TUnityContainerService>(
            this IUnityDependency<TNativeContainer, TUnityContainerService> dependency,
            MonoBehaviour unityContained,
            IUnityContainer<TNativeContainer> overrideContainer,
            Action onRegistration)
            where TNativeContainer : IDependencyContainer, IDependencyResolutionProvider, IDisposable
            where TUnityContainerService : IUnityContainerService<TNativeContainer>, new()
        {
            dependency.SetContainer(unityContained, overrideContainer);
            onRegistration?.Invoke();
        }

        /// <summary>
        /// Updates the container of the dependency based on its current transform hierarchy, 
        /// and invokes the provided change handler and registration method upon a container change.
        /// </summary>
        /// <remarks>
        /// Usually called as part of 
        /// <see cref="IUnityDependent{TNativeContainer, TUnityContainerService}.OnTransformParentChanged"/>.
        /// </remarks>
        /// <typeparam name="TNativeContainer">The type of the internal, non-Unity 
        /// dependency container.</typeparam>
        /// <typeparam name="TUnityContainerService">The type of the Unity container service that 
        /// provides Unity-specific services.</typeparam>
        /// <param name="dependency">This dependency that is having its container set.</param>
        /// <param name="unityContained">The MonoBehaviour of this dependency.</param>
        /// <param name="overrideContainer">The container that may be used as an override 
        /// when setting the container.</param>
        /// <param name="onPreContainerChanged">The method called prior to when the container 
        /// is being updated.</param>
        /// <param name="onPreContainerChanged">The method called after the container 
        /// has been updated.</param>
        /// <param name="onRegistration">The method called when the container has been set, 
        /// to initiate registration of the dependency as its contracts.
        public static void UpdateContainer<TNativeContainer, TUnityContainerService>(
            this IUnityDependency<TNativeContainer, TUnityContainerService> dependency,
            MonoBehaviour unityContained,
            IUnityContainer<TNativeContainer> overrideContainer,
            Action onPreContainerChanged,
            Action onPostContainerChanged,
            Action onRegistration)
            where TNativeContainer : IDependencyContainer, IDependencyResolutionProvider, IDisposable
            where TUnityContainerService : IUnityContainerService<TNativeContainer>, new()
        {
            dependency.UpdateContainer(unityContained, overrideContainer,
                () =>
                {
                    dependency.DeregisterAll();
                    onPreContainerChanged?.Invoke();
                },
                () =>
                {
                    onPostContainerChanged?.Invoke();
                    onRegistration?.Invoke();
                });
        }


        public static void DeregisterAll<TNativeContainer, TUnityContainerService>(
            this IUnityDependency<TNativeContainer, TUnityContainerService> dependency)
            where TNativeContainer : IDependencyContainer, IDependencyResolutionProvider, IDisposable
            where TUnityContainerService : IUnityContainerService<TNativeContainer>, new()
        {
            foreach (var registration in dependency.Registrations)
                dependency.Container.Deregister(registration);
            dependency.Registrations.Clear();
        }

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
