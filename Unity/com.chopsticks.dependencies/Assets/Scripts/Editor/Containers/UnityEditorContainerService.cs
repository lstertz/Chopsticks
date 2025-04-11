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
        /// Finds the parent Unity container of the given MonoBehaviour, 
        /// per the specified <see cref="ContainerRetrievalSetting"/>.
        /// </summary>
        /// <typeparam name="TUnityContainer">The type of containing parent 
        /// will be searched for.</typeparam>
        /// <param name="setting">The setting that defines the strategy applied 
        /// to find the parent container.</param>
        /// <param name="unityContainer">The MonoBehaviour whose parent will be 
        /// searched for.</param>
        /// <param name="overrideContainer">The wrapping Unity container of a container 
        /// that may be provided per some settings.</param>
        /// <returns>The found parent container, as a MonoBehaviour.</returns>
        public static MonoBehaviour FindParentUnityContainer<
            TUnityContainer, TOverrideContainer>(
            ContainerSetting setting, MonoBehaviour unityContainer, 
            TOverrideContainer overrideContainer)
            where TUnityContainer : MonoBehaviour
            where TOverrideContainer : MonoBehaviour =>
            setting switch
            {
                ContainerSetting.HierarchyWithGlobal =>
                    unityContainer.transform.parent == null ? null : 
                        unityContainer.transform.parent.GetComponentInParent<TUnityContainer>(),
                ContainerSetting.HierarchyWithoutGlobal =>
                    unityContainer.transform.parent == null ? null : 
                        unityContainer.transform.parent.GetComponentInParent<TUnityContainer>(),
                ContainerSetting.Global => null,
                ContainerSetting.Override => overrideContainer,
                ContainerSetting.None => null,
                _ => throw new NotSupportedException($"The container retrieval setting of " +
                                        $"{setting} is not supported."),
            };
    }
} 