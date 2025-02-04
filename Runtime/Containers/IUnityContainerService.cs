using Chopsticks.Dependencies.Resolutions;
using System;
using UnityEngine;

namespace Chopsticks.Dependencies.Containers
{
    /// <summary>
    /// Provides Unity-specific services for working with containers, including 
    /// access to a global instance and strategies to work with other containers.
    /// </summary>
    /// <typeparam name="TNativeContainer">The type of the native container for which 
    /// Unity-specific services are being provided.</typeparam>
    /// <typeparam name="TNativeContainerDefinition">The type of container 
    /// definition used to create the global container.</typeparam>
    public interface IUnityContainerService<TNativeContainer, TNativeContainerDefinition>
        where TNativeContainer : IDependencyContainer, IDependencyResolutionProvider, IDisposable
    {
        /// <summary>
        /// A static, global instance of a container, for use as a default or 
        /// the highest-level container of a hierarchy of containers.
        /// </summary>
        TNativeContainer GlobalContainer { get; }


        /// <summary>
        /// Finds the parent container of the given Unity container, 
        /// per the specified <see cref="ContainerRetrievalSetting"/>.
        /// </summary>
        /// <typeparam name="TUnityContainer">The type of the Unity container whose 
        /// parent will be searched for.</typeparam>
        /// <typeparam name="TOverrideContainer">The type of the container that may 
        /// be provided as an override, per some settings.</typeparam>
        /// <param name="setting">The setting that defines the strategy applied 
        /// to find the parent container.</param>
        /// <param name="unityContainer">The Unity container whose parent will be 
        /// searched for.</param>
        /// <param name="overrideContainer">The wrapping Unity container of a contaienr 
        /// that may be provided per some settings.</param>
        /// <returns>The found parent container, or null if either no such 
        /// container could be found or if the specified override is actually a child of the 
        /// provided Unity container.</returns>
        TNativeContainer FindParentContainer<TUnityContainer, TOverrideContainer>(
            ContainerRetrievalSetting setting, TUnityContainer unityContainer, 
            TOverrideContainer overrideContainer)
            where TUnityContainer : MonoBehaviour, IUnityContainer<TNativeContainer>
            where TOverrideContainer : IUnityContainer<TNativeContainer>;

        /// <summary>
        /// Provides a dependency container per the specified 
        /// <see cref="ContainerRetrievalSetting"/>, starting from the given Unity container.
        /// </summary>
        /// <typeparam name="TUnityContainer">The type of the Unity container from 
        /// which retrieval will start, per some settings.</typeparam>
        /// <typeparam name="TOverrideContainer">The type of the container that may 
        /// be used as an override, per some settings.</typeparam>
        /// <param name="setting">The setting that defines the strategy applied 
        /// to retrieve a container.</param>
        /// <param name="includeSelf">Whether the provided Unity container considers 
        /// itself for some retrieval strategies, particularly when retrieval 
        /// involves a hierarchy.</param>
        /// <param name="unityContainer">The Unity container from which 
        /// retrieval will start, per some settings.</param>
        /// <param name="overrideContainer">The wrapping Unity container of a container 
        /// that may be retrieved per some settings.</param>
        /// <returns>A container retrieved per the specified setting, 
        /// or null if no such container could be found.</returns>
        TNativeContainer GetContainer<TUnityContainer, TOverrideContainer>(
            ContainerRetrievalSetting setting, bool includeSelf, 
            TUnityContainer unityContainer, TOverrideContainer overrideContainer)
            where TUnityContainer : MonoBehaviour, IUnityContainer<TNativeContainer>
            where TOverrideContainer : IUnityContainer<TNativeContainer>;

        /// <summary>
        /// Resets the <see cref="GlobalContainer"/>.
        /// </summary>
        /// <param name="definition">The optional definition to specify the 
        /// settings of the container.</param>
        void ResetGlobal(TNativeContainerDefinition definition = default);
    }
}
