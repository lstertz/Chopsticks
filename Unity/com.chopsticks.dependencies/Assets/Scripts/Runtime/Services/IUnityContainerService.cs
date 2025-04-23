using Chopsticks.Dependencies.Containers;
using Chopsticks.Dependencies.Resolutions;
using System;
using UnityEngine;

namespace Chopsticks.Dependencies.Services
{
    /// <summary>
    /// Provides services for working with Unity containers.
    /// </summary>
    /// <typeparam name="TNativeContainer">The type of the native container for which 
    /// Unity-specific services are being provided.</typeparam>
    public interface IUnityContainerService<TNativeContainer>
        where TNativeContainer : IDependencyContainer, IDependencyResolutionProvider, IDisposable
    {
        /// <summary>
        /// Finds the parent container of the given Unity container, 
        /// per the specified <see cref="ContainerSetting"/>.
        /// </summary>
        /// <typeparam name="TUnityContainer">The type of the Unity container whose 
        /// parent will be searched for.</typeparam>
        /// <typeparam name="TOverrideContainer">The type of the container that may 
        /// be provided as an override, per some settings.</typeparam>
        /// <param name="setting">The setting that defines the strategy applied 
        /// to find the parent container.</param>
        /// <param name="unityContainer">The Unity container whose parent will be 
        /// searched for.</param>
        /// <param name="overrideContainer">The wrapping Unity container of a container 
        /// that may be provided per some settings.</param>
        /// <returns>The found parent container, or null if either no such 
        /// container could be found or if the specified override is actually a child of the 
        /// provided Unity container.</returns>
        TNativeContainer FindParentContainer<TUnityContainer, TOverrideContainer>(
            ContainerSetting setting, TUnityContainer unityContainer,
            TOverrideContainer overrideContainer)
            where TUnityContainer : MonoBehaviour, IUnityContainer<TNativeContainer>
            where TOverrideContainer : IUnityContainer<TNativeContainer>;

        /// <summary>
        /// Provides a dependency container per the specified 
        /// <see cref="ContainerSetting"/>, starting from the given contained 
        /// Unity construct.
        /// </summary>
        /// <typeparam name="TOverrideContainer">The type of the container that may 
        /// be used as an override, per some settings.</typeparam>
        /// <param name="setting">The setting that defines the strategy applied 
        /// to retrieve a container.</param>
        /// <param name="includeSelf">Whether the provided Unity container considers 
        /// itself for some retrieval strategies, particularly when retrieval 
        /// involves a hierarchy.</param>
        /// <param name="unityContained">The contained Unity transform from which 
        /// retrieval will start, per some settings.</param>
        /// <param name="overrideContainer">The wrapping Unity container of a container 
        /// that may be retrieved per some settings.</param>
        /// <returns>A container retrieved per the specified setting, 
        /// or null if no such container could be found.</returns>
        TNativeContainer GetContainer<TOverrideContainer>(
            ContainerSetting setting, bool includeSelf,
            Transform unityContained, TOverrideContainer overrideContainer)
            where TOverrideContainer : IUnityContainer<TNativeContainer>;

        /// <summary>
        /// Provides a dependency container from the parent hierarchy of the 
        /// provided contained Unity transform.
        /// </summary>
        /// <param name="includeSelf">Whether the provided Unity container considers 
        /// itself to be part of the hierarchy to be searched.</param>
        /// <param name="fallbackToGlobal">Whether the global container will be 
        /// provided if there is no other container in the hierarchy.</param>
        /// <param name="unityContained">The contained Unity transform from which 
        /// retrieval will start.</param>
        /// <returns>A container retrieved, or null if no such container could be found.</returns>
        TNativeContainer GetContainerFromHierarchy(Transform unityContained,
            bool includeSelf = true, bool fallbackToGlobal = true);
    }


    /// <summary>
    /// Provides services for working with Unity containers, 
    /// including strategies to work with other containers.
    /// </summary>
    /// <typeparam name="TNativeContainer">The type of the native container for which 
    /// Unity-specific services are being provided.</typeparam>
    /// <typeparam name="TNativeContainerDefinition">The type of container 
    /// definition used to create native containers, including the global container.</typeparam>
    public interface IUnityContainerService<TNativeContainer, TNativeContainerDefinition> : 
        IUnityContainerService<TNativeContainer>
        where TNativeContainer : IDependencyContainer, IDependencyResolutionProvider, IDisposable
    {
        /// <summary>
        /// Builds a new native container as an abstraction from a Unity container.
        /// </summary>
        /// <param name="definition">The optional definition to specify the 
        /// settings of the container.</param>
        /// <returns>The new native container.</returns>
        TNativeContainer BuildContainer(TNativeContainerDefinition definition = default);
    }
}
