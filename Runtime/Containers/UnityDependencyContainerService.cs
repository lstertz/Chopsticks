using Chopsticks.Dependencies.Factories;
using Chopsticks.Dependencies.Resolutions;
using System;
using UnityEngine;

namespace Chopsticks.Dependencies.Containers
{
    /// <inheritdoc cref="IUnityContainerService{TNativeContainer, TNativeContainerDefinition}"/>
    /// <typeparam name="TNativeContainerFactory">The type of the factory 
    /// that creates the global container of the service.</typeparam>
    /// <typeparam name="TNativeContainerDefinition">The type of container 
    /// definition used to create the global container.</typeparam>
    /// <remarks>
    /// By default, the <see cref="GlobalContainer"/> will be the default container 
    /// built by the specified factory. <see cref="ResetGlobal"/> can be used to specify 
    /// the settings of a new container per a provided <see cref="TNativeContainerDefinition"/>.
    /// </remarks>
    public class UnityContainerService<TNativeContainer, TNativeContainerFactory, 
        TNativeContainerDefinition> : 
        IUnityContainerService<TNativeContainer, TNativeContainerDefinition>
        where TNativeContainer : IDependencyContainer, IDependencyResolutionProvider, IDisposable
        where TNativeContainerFactory : IDependencyContainerFactory<TNativeContainer, 
            TNativeContainerDefinition>, new()
    {
        /// <inheritdoc/>
        public TNativeContainer GlobalContainer => _instance;

        private static readonly TNativeContainerFactory _instanceFactory = new();
        private static TNativeContainer _instance = _instanceFactory.BuildContainer();



        /// <inheritdoc/>
        /// <exception cref="NotSupportedException">Thrown if a  
        /// <see cref="ContainerRetrievalSetting"/>is not supported.</exception>
        public TNativeContainer FindParentContainer<TUnityContainer, TOverrideContainer>(
            ContainerRetrievalSetting setting, TUnityContainer unityContainer,
            TOverrideContainer overrideContainer)
            where TUnityContainer : MonoBehaviour, IUnityContainer<TNativeContainer>
            where TOverrideContainer : IUnityContainer<TNativeContainer> =>
            setting switch
            {
                ContainerRetrievalSetting.HierarchyWithGlobal =>
                    GetContainer(setting, false, unityContainer, overrideContainer),
                ContainerRetrievalSetting.HierarchyWithoutGlobal =>
                    GetContainer(setting, false, unityContainer, overrideContainer),
                ContainerRetrievalSetting.Global => GlobalContainer,
                ContainerRetrievalSetting.Override =>
                    ValidateOverrideParent(unityContainer, overrideContainer) == null ? default : 
                        overrideContainer.NativeContainer,
                _ => throw new NotSupportedException($"The container retrieval setting of " +
                                        $"{setting} is not supported."),
            };

        /// <inheritdoc/>
        /// <exception cref="NotSupportedException">Thrown if a  
        /// <see cref="ContainerRetrievalSetting"/>is not supported.</exception>
        public TNativeContainer GetContainer<TUnityContainer, TOverrideContainer>(
            ContainerRetrievalSetting setting, bool includeSelf, 
            TUnityContainer unityContainer, TOverrideContainer overrideContainer)
            where TUnityContainer : MonoBehaviour, IUnityContainer<TNativeContainer>
            where TOverrideContainer : IUnityContainer<TNativeContainer> =>
            setting switch
            {
                ContainerRetrievalSetting.HierarchyWithGlobal =>
                    FindContainerInHierarchy(includeSelf ? unityContainer.transform : 
                        unityContainer.transform.parent, true),
                ContainerRetrievalSetting.HierarchyWithoutGlobal =>
                    FindContainerInHierarchy(includeSelf ? unityContainer.transform :
                        unityContainer.transform.parent, false),
                ContainerRetrievalSetting.Global => 
                    GlobalContainer,
                ContainerRetrievalSetting.Override => 
                    overrideContainer == null ? default : overrideContainer.NativeContainer,
                _ => throw new NotSupportedException($"The container retrieval setting of " +
                                        $"{setting} is not supported."),
            };

        /// <inheritdoc/>
        public void ResetGlobal(TNativeContainerDefinition definition = default)
        {
            _instance?.Dispose();
            _instance = _instanceFactory.BuildContainer(definition);
        }


        private TNativeContainer FindContainerInHierarchy(
            Transform transform, bool defaultToGlobal)
        {
            var container = transform == null ? null :
                    transform.GetComponentInParent<IUnityContainer<TNativeContainer>>();

            if (container == null)
            {
                if (defaultToGlobal)
                    return GlobalContainer;
                return default;
            }

            return container.NativeContainer;
        }

        private TOverrideContainer ValidateOverrideParent<TUnityContainer, TOverrideContainer>(
            TUnityContainer child, TOverrideContainer overrideParent)
            where TUnityContainer : IUnityContainer<TNativeContainer>
            where TOverrideContainer : IUnityContainer<TNativeContainer>
        {
            if (overrideParent == null)
                return default;

            IDependencyResolutionProvider parent = overrideParent.NativeContainer;
            while (parent != null)
            {
                if (parent.Equals(child.NativeContainer))
                    return default;

                parent = parent.Parent;
            }

            return overrideParent;
        }
    }
}
