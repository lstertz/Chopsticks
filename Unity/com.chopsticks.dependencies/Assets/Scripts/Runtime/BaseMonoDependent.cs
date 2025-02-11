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
        protected ContainerSetting _containerSetting;

        [SerializeField]
        private BaseUnityContainer<TNativeContainer> _overrideContainer;


        // TODO :: Extract all functionality to extensions.

        ///<inheritdoc/>
        public virtual void OnEnable()
        {
            GetUpdatedContainer(out var updatedContainer);
            (this as IUnityContained<TNativeContainer>).Container = updatedContainer;
        }

        ///<inheritdoc/>
        public virtual void OnTransformParentChanged()
        {
            if (!enabled)
                return;

            if (!GetUpdatedContainer(out var updatedContainer))
                return;

            (this as IUnityContained<TNativeContainer>).Container = updatedContainer;
            OnContainerChanged();
        }

        /// <summary>
        /// Invoked when the container is changed as the result of a change in the 
        /// MonoBehaviour's parent hierarchy.
        /// </summary>
        protected virtual void OnContainerChanged() { }


        protected TContract AssertiveGetOrResolve<TContract>(string customErrorMessage = "")
        {

        }

        protected TContract AssertiveResolve<TContract>(string customErrorMessage = "")
        {

        }

        protected bool GetUpdatedContainer(out TNativeContainer updatedContainer)
        {
            updatedContainer = this.FindContainer(transform, _overrideContainer);
            // TODO :: Handle None.

            return !(this as IUnityContained<TNativeContainer>).Container.Equals(updatedContainer);
        }

        protected bool GetOrResolve<TContract>(out TContract implementation)
        {
            //Container.Resolve<TContract>()
        }

        protected IEnumerable<TContract> GetOrResolveAll<TContract>()
        {

        }

        protected bool Resolve<TContract>(out TContract implementation)
        {

        }

        protected IEnumerable<TContract> ResolveAll<TContract>()
        {

        }
    }
}
