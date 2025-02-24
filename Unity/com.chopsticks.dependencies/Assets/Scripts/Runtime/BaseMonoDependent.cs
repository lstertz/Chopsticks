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
            this.SetContainer(this, _overrideContainer);

        ///<inheritdoc/>
        public virtual void OnTransformParentChanged() => 
            this.UpdateContainer(this, _overrideContainer, 
                OnPreContainerChanged, OnPostContainerChanged);


        /// <summary>
        /// Invoked after to the container has been changed as the result of a change in the 
        /// MonoBehaviour's parent hierarchy.
        /// </summary>
        /// <remarks>
        /// This is not called if the MonoBehaviour is disabled. Handle any possible 
        /// container changes that occur while disabled by extending the existing 
        /// functionality of <see cref="OnEnable"/>.
        /// </remarks>
        protected virtual void OnPostContainerChanged() { }

        /// <summary>
        /// Invoked prior to the container being changed as the result of a change in the 
        /// MonoBehaviour's parent hierarchy.
        /// </summary>
        /// <remarks>
        /// This is not called if the MonoBehaviour is disabled. Handle any possible 
        /// container changes that occur while disabled by extending the existing 
        /// functionality of <see cref="OnEnable"/>.
        /// </remarks>
        protected virtual void OnPreContainerChanged() { }


        /// <inheritdoc cref="IUnityContainedExtensions
        /// .AssertiveResolve{TNativeContainer, TContract}(IUnityContained{TNativeContainer}, string)"/>
        protected TContract AssertiveResolve<TContract>(string customErrorMessage = "") => 
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
