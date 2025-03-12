using Chopsticks.Dependencies.Contained;
using Chopsticks.Dependencies.Containers;
using Chopsticks.Dependencies.Resolutions;
using Chopsticks.Dependencies.Services;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Chopsticks.Dependencies
{
    ///<inheritdoc cref="IUnityDependent{TNativeContainer, TUnityContainerService}"/>
    public abstract class BaseMonoDependent<TNativeContainer, TUnityContainerService> :
        MonoBehaviour, IUnityDependent<TNativeContainer, TUnityContainerService>
        where TNativeContainer : IDependencyContainer, IDependencyResolutionProvider, IDisposable
        where TUnityContainerService : IUnityContainerService<TNativeContainer>, new()
    {
        ///<inheritdoc/>
        TNativeContainer IUnityContained<TNativeContainer>.Container { get; set; }

        ContainerSetting IUnityContained<TNativeContainer>.ContainerSetting => _containerSetting;
        [SerializeField]
        protected ContainerSetting _containerSetting = ContainerSetting.HierarchyWithGlobal;

        /// <summary>
        /// The container that may serve as an overriding container for this dependent.
        /// </summary>
        protected BaseUnityContainer<TNativeContainer> OverrideContainer => _overrideContainer;
        [SerializeField]
        private BaseUnityContainer<TNativeContainer> _overrideContainer;


        ///<inheritdoc/>
        public virtual void OnEnable() =>
            this.UpdateContainer(this, _overrideContainer,
                null, ResolveDependencies);

        ///<inheritdoc/>
        public virtual void OnTransformParentChanged() => 
            this.UpdateContainer(this, _overrideContainer, 
                null, ResolveDependencies);


        /// <summary>
        /// Invoked after to the container has been changed for resolving 
        /// the dependencies of the dependent.
        /// </summary>
        /// <remarks>
        /// Cached dependencies should be re-resolved and re-cached here since 
        /// the container that may have provided them could have changed.
        /// </remarks>
        protected virtual void ResolveDependencies() { }


        /// <inheritdoc cref="IUnityContainedExtensions
        /// .AssertiveResolve{TNativeContainer, TContract}(IUnityContained{TNativeContainer}, string)"/>
        protected TContract AssertiveResolve<TContract>(string customErrorMessage = null) => 
            this.AssertiveResolve<TNativeContainer, TContract>(customErrorMessage);

        /// <inheritdoc cref="IUnityContainedExtensions
        /// .Resolve{TNativeContainer, TContract}(IUnityContained{TNativeContainer}, out TContract)"/>
        protected bool Resolve<TContract>(out TContract implementation) =>
            this.Resolve<TNativeContainer, TContract>(out implementation);

        /// <inheritdoc cref="IUnityContainedExtensions
        /// .ResolveAll{TNativeContainer, TContract}(IUnityContained{TNativeContainer})"/>
        protected IEnumerable<TContract> ResolveAll<TContract>() =>
            this.ResolveAll<TNativeContainer, TContract>();
    }
}
