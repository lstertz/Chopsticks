using Chopsticks.Dependencies.Containers;
using Chopsticks.Dependencies.Resolutions;
using System;
using UnityEngine;

namespace Chopsticks.Dependencies.Consumers
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
            where TOverrideContainer : IUnityContainer<TNativeContainer>
        {
            var service = IUnityDependent<TNativeContainer, TUnityContainerService>
                .UnityContainerService;
            service.GetContainer((ContainerRetrievalSetting)dependent.ContainerSetting,
                true, unityContained, overrideContainer);

            // TODO :: Implement.
            return default;
        }

        // TODO :: Resolve extension to get dependencies.
    }
}
