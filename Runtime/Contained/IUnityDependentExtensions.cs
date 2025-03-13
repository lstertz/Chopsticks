using Chopsticks.Dependencies.Containers;
using Chopsticks.Dependencies.Resolutions;
using Chopsticks.Dependencies.Services;
using System;
using UnityEngine;

namespace Chopsticks.Dependencies.Contained
{
    /// <summary>
    /// Provides extensions to Unity dependents.
    /// </summary>
    public static class IUnityDependentExtensions
    {
        /// <summary>
        /// Finds the Unity container of this dependent.
        /// </summary>
        /// <typeparam name="TNativeContainer">The type of the internal, non-Unity 
        /// dependency container.</typeparam>
        /// <typeparam name="TUnityContainerService">The type of the Unity container service that 
        /// provides Unity-specific services.</typeparam>
        /// <typeparam name="TOverrideContainer">The type of the overriding Unity container 
        /// that may be provided per some settings on the dependent.</typeparam>
        /// <param name="dependent">The dependent whose container is to be found.</param>
        /// <param name="overrideContainer">The overriding container that may be 
        /// returned per some settings on the dependent.</param>
        /// <returns>The appropriate Unity container for this dependent, or null if no such 
        /// container could be found.</returns>
        public static TNativeContainer FindContainer<TNativeContainer,
            TUnityContainerService, TOverrideContainer>(
                this IUnityDependent<TNativeContainer, TUnityContainerService> dependent,
                Transform unityContained,
                TOverrideContainer overrideContainer)
            where TNativeContainer : IDependencyContainer, IDependencyResolutionProvider, IDisposable
            where TUnityContainerService : IUnityContainerService<TNativeContainer>, new()
            where TOverrideContainer : IUnityContainer<TNativeContainer> =>
                dependent.Service.GetContainer(
                    (ContainerRetrievalSetting)dependent.ContainerSetting,
                    true, unityContained, overrideContainer);

        /// <summary>
        /// Updates the container of the dependent based on its current transform hierarchy, 
        /// and invokes the provided change handlers upon a container change.
        /// </summary>
        /// <remarks>
        /// Usually called as part of 
        /// <see cref="IUnityDependent{TNativeContainer, TUnityContainerService}.OnTransformParentChanged"/>.
        /// </remarks>
        /// <param name="dependent">This dependent that is having its container set.</param>
        /// <param name="unityContained">The MonoBehaviour of this dependent.</param>
        /// <param name="overrideContainer">The container that may be used as an override 
        /// when setting the container.</param>
        /// <param name="onPreContainerChanged">The method called prior to when the container 
        /// is being updated.</param>
        /// <param name="onPostContainerChanged">The method called after the container 
        /// has been updated, generally to resolve dependencies or similar functionality.</param>
        public static void UpdateContainer<TNativeContainer, TUnityContainerService>(
            this IUnityDependent<TNativeContainer, TUnityContainerService> dependent,
            MonoBehaviour unityContained,
            IUnityContainer<TNativeContainer> overrideContainer, 
            Action onPreContainerChanged, Action onPostContainerChanged)
            where TNativeContainer : IDependencyContainer, IDependencyResolutionProvider, IDisposable
            where TUnityContainerService : IUnityContainerService<TNativeContainer>, new()
        {
            if (!unityContained.enabled)
                return;

            TNativeContainer updatedContainer = default;
            if (dependent.ContainerSetting != ContainerSetting.None)
                updatedContainer = dependent.Service.GetContainer(
                    (ContainerRetrievalSetting)dependent.ContainerSetting,
                    true, unityContained.transform, overrideContainer);

            if (dependent.Container == null)
            {
                if (updatedContainer == null)
                    return;
            }
            else if (dependent.Container.Equals(updatedContainer))
                return;

            onPreContainerChanged?.Invoke();
            dependent.Container = updatedContainer;
            onPostContainerChanged?.Invoke();
        }
    }
}
