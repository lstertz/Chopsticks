using UnityEngine;
using System;

namespace Chopsticks.Dependencies.Containers
{
    /// <summary>
    /// Provides Unity Editor-specific services for working with containers.
    /// </summary>
    public static class UnityEditorContainerService
    {
        /// <summary>
        /// Finds the parent Unity container of the given Unity container, 
        /// per the specified <see cref="ContainerRetrievalSetting"/>.
        /// </summary>
        /// <typeparam name="TUnityContainer">The type of the Unity container whose 
        /// same typed parent will be searched for.</typeparam>
        /// <param name="setting">The setting that defines the strategy applied 
        /// to find the parent container.</param>
        /// <param name="unityContainer">The Unity container whose parent will be 
        /// searched for.</param>
        /// <param name="overrideContainer">The wrapping Unity container of a container 
        /// that may be provided per some settings.</param>
        /// <returns>The found parent container, or null if either no such 
        /// container could be found or if the specified override is actually a child of the 
        /// provided Unity container.</returns>
        public static IUnityContainerEditor FindParentUnityContainer<
            TUnityContainer, TOverrideContainer>(
            ContainerRetrievalSetting setting, TUnityContainer unityContainer, 
            TOverrideContainer overrideContainer)
            where TUnityContainer : MonoBehaviour, IUnityContainerEditor
            where TOverrideContainer : MonoBehaviour, IUnityContainerEditor =>
            setting switch
            {
                ContainerRetrievalSetting.HierarchyWithGlobal =>
                    unityContainer.transform.parent == null ? null : 
                        unityContainer.transform.parent.GetComponentInParent<TUnityContainer>(),
                ContainerRetrievalSetting.HierarchyWithoutGlobal =>
                    unityContainer.transform.parent == null ? null : 
                        unityContainer.transform.parent.GetComponentInParent<TUnityContainer>(),
                ContainerRetrievalSetting.Global => null,
                ContainerRetrievalSetting.Override => overrideContainer,
                _ => throw new NotSupportedException($"The container retrieval setting of " +
                                        $"{setting} is not supported."),
            };
    }
} 