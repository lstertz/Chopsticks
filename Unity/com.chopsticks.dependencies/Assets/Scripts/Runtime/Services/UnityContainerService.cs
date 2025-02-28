using Chopsticks.Dependencies.Containers;
using Chopsticks.Dependencies.Factories;
using Chopsticks.Dependencies.Resolutions;
using System;
using UnityEngine;

namespace Chopsticks.Dependencies.Services
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
        /// <summary>
        /// A static, global instance of a container, for use as a default or 
        /// the highest-level container of a hierarchy of containers.
        /// </summary>
        public static TNativeContainer GlobalContainer => _globalContainer;

        protected static readonly TNativeContainerFactory _instanceFactory = new();
        protected static TNativeContainer _globalContainer = _instanceFactory.BuildContainer();


        /// <summary>
        /// Resets the <see cref="GlobalContainer"/>.
        /// </summary>
        /// <param name="definition">The optional definition to specify the 
        /// settings of the container.</param>
        public static void ResetGlobal(TNativeContainerDefinition definition = default)
        {
            _globalContainer?.Dispose();
            _globalContainer = _instanceFactory.BuildContainer(definition);
        }


        /// <inheritdoc/>
        public virtual TNativeContainer BuildContainer(
            TNativeContainerDefinition definition = default) => 
                _instanceFactory.BuildContainer(definition);

        /// <inheritdoc/>
        /// <exception cref="NotSupportedException">Thrown if a  
        /// <see cref="ContainerRetrievalSetting"/>is not supported.</exception>
        public virtual TNativeContainer FindParentContainer<TUnityContainer, TOverrideContainer>(
            ContainerRetrievalSetting setting, TUnityContainer unityContainer,
            TOverrideContainer overrideContainer)
            where TUnityContainer : MonoBehaviour, IUnityContainer<TNativeContainer>
            where TOverrideContainer : IUnityContainer<TNativeContainer> =>
            setting switch
            {
                ContainerRetrievalSetting.HierarchyWithGlobal =>
                    GetContainer(setting, false, unityContainer.transform, overrideContainer),
                ContainerRetrievalSetting.HierarchyWithoutGlobal =>
                    GetContainer(setting, false, unityContainer.transform, overrideContainer),
                ContainerRetrievalSetting.Global => 
                    _globalContainer,
                ContainerRetrievalSetting.Override =>
                    ValidateOverrideParent(unityContainer, overrideContainer) == null ? default : 
                        overrideContainer.NativeContainer,
                _ => throw new NotSupportedException($"The container retrieval setting of " +
                                        $"{setting} is not supported."),
            };

        /// <inheritdoc/>
        /// <exception cref="NotSupportedException">Thrown if a  
        /// <see cref="ContainerRetrievalSetting"/>is not supported.</exception>
        public virtual TNativeContainer GetContainer<TOverrideContainer>(
            ContainerRetrievalSetting setting, bool includeSelf, 
            Transform unityContained, TOverrideContainer overrideContainer)
            where TOverrideContainer : IUnityContainer<TNativeContainer> =>
            setting switch
            {
                ContainerRetrievalSetting.HierarchyWithGlobal =>
                    FindContainerInHierarchy(includeSelf ? 
                        unityContained : unityContained.parent, true),
                ContainerRetrievalSetting.HierarchyWithoutGlobal =>
                    FindContainerInHierarchy(includeSelf ? 
                        unityContained : unityContained.parent, false),
                ContainerRetrievalSetting.Global =>
                    _globalContainer,
                ContainerRetrievalSetting.Override => 
                    overrideContainer == null ? default : overrideContainer.NativeContainer,
                _ => throw new NotSupportedException($"The container retrieval setting of " +
                                        $"{setting} is not supported."),
            };

        /// <inheritdoc/>
        public virtual TNativeContainer GetContainerFromHierarchy(Transform unityContained,
            bool includeSelf = true, bool fallbackToGlobal = true) =>
                FindContainerInHierarchy(includeSelf ?
                    unityContained : unityContained.parent, fallbackToGlobal);


        private TNativeContainer FindContainerInHierarchy(
            Transform transform, bool fallbackToGlobal)
        {
            var container = transform == null ? null :
                    transform.GetComponentInParent<IUnityContainer<TNativeContainer>>();

            if (container == null)
            {
                if (fallbackToGlobal)
                    return _globalContainer;
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
