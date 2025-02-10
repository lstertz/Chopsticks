using Chopsticks.Dependencies.Factories;
using Chopsticks.Dependencies.Resolutions;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Chopsticks.Dependencies.Containers
{
    /// <summary>
    /// Manages the organization of MonoBehaviour containers based on the hierarchy of GameObjects.
    /// </summary>
    /// <remarks>
    /// Instantiation of this MonoBehaviour will not automatically update the set parent container 
    /// of any pre-existing hierarchical child containers.
    /// </remarks>
    /// <typeparam name="TNativeContainer">The type of the internal, non-Unity 
    /// dependency container.</typeparam>
    /// <typeparam name="TNativeContainerFactory">The factory to produce the internal, non-Unity 
    /// dependency container.</typeparam>
    /// <typeparam name="TNativeContainerDefinition">The type of definition to define any custom 
    /// properties of the internal, non-Unity dependency container.</typeparam>
    /// <typeparam name="TUnityContainerService">The type of the Unity container service that 
    /// provides Unity-specific services.</typeparam>
    public abstract class BaseMonoContainer<TNativeContainer, TNativeContainerFactory,
        TNativeContainerDefinition, TUnityContainerService> :
        BaseUnityContainer<TNativeContainer>
        where TNativeContainer : IDependencyContainer, IDependencyResolutionProvider, IDisposable
        where TNativeContainerFactory : IDependencyContainerFactory<TNativeContainer,
            TNativeContainerDefinition>, new()
        where TUnityContainerService : IUnityContainerService<TNativeContainer, 
            TNativeContainerDefinition>, new()
    {
        /// <summary>
        /// The global (highest application scope) container for all of the same type 
        /// of MonoContainers, as defined by this container's Unity Container Service.
        /// </summary>
        public static IDependencyContainer Global => _containerService.GlobalContainer;

        protected static readonly TUnityContainerService _containerService = new();
        protected static readonly TNativeContainerFactory _containerFactory = new();


        /// <inheritdoc/>
        protected override TNativeContainer InternalContainer =>
            _internalContainer ??= _containerFactory.BuildContainer(InternalContainerDefinition);
        private TNativeContainer _internalContainer;

        /// <summary>
        /// The definition used, by default, to define the <see cref="InternalContainer"/>.
        /// </summary>
        protected virtual TNativeContainerDefinition InternalContainerDefinition { get; }

        /// <inheritdoc/>
        protected override ContainerSetting ParentContainerSetting => _containerParentSetting;
        [SerializeField]
        private ContainerSetting _containerParentSetting = ContainerSetting.HierarchyWithGlobal;


        [SerializeField]
        private bool _inheritParentDependencies = true;

        [SerializeField]
        private BaseUnityContainer<TNativeContainer> _overrideParent;


        // TODO :: Inspector display features:
        //          Contained MonoDependencies.
        //          Maybe list native dependencies.



        /// <summary>
        /// Defines parent settings and calls <see cref="RegisterNativeDependencies"/>.
        /// </summary>
        public void Awake()
        {
            InternalContainer.InheritParentDependencies = _inheritParentDependencies;
            UpdateParent();

            RegisterNativeDependencies();
        }

        /// <summary>
        /// Registers any native dependencies that should be inherent to this container.
        /// </summary>
        /// <remarks>
        /// This is only performed once during <see cref="Awake"/>.
        /// </remarks>
        protected virtual void RegisterNativeDependencies() { }


        /// <summary>
        /// Disposes of the internal container, releasing the resources of 
        /// its managed dependencies.
        /// </summary>
        public void OnDestroy() => 
            InternalContainer.Dispose();

        /// <summary>
        /// Verifies any changes to the container's parent, per any 
        /// changes in the GameObject hierarchy, if appropriate.
        /// </summary>
        public void OnTransformParentChanged() => 
            UpdateParent();

        /// <summary>
        /// Verifies any changes to the container's parent, per any 
        /// changes to the parent settings.
        /// </summary>
        public void OnValidate() =>
            UpdateParent();


        /// <inheritdoc/>
        public override IDependencyContainer Deregister(DependencyRegistration registration)
        {
            InternalContainer.Deregister(registration);
            return this;
        }

        /// <inheritdoc/>
        public override IDependencyContainer Register(DependencySpecification specification,
            out DependencyRegistration registration)
        {
            InternalContainer.Register(specification, out registration);
            return this;
        }

        /// <inheritdoc/>
        public override bool Resolve(Type dependencyType, out object implementation) =>
            InternalContainer.Resolve(dependencyType, out implementation);

        /// <inheritdoc/>
        public override IEnumerable<object> ResolveAll(Type dependencyType) =>
            InternalContainer.ResolveAll(dependencyType);


        private void UpdateParent()
        {
            if (_containerParentSetting == ContainerSetting.None)
            {
                InternalContainer.Parent = null;
                return;
            }

            InternalContainer.Parent = _containerService.FindParentContainer(
                (ContainerRetrievalSetting)_containerParentSetting, this, _overrideParent);

            if (_containerParentSetting == ContainerSetting.Override &&
                InternalContainer.Parent == null && _overrideParent != null)
            {
                Debug.LogError($"BaseMonoContainer :: Override parent was reset to 'null' " +
                    $"on the GameObject called '{gameObject.name}' to prevent a parent loop " +
                    $"with the GameObject called '{_overrideParent.name}'.");
                _overrideParent = null;
            }
        }
    }
}
