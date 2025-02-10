using Chopsticks.Dependencies.Contained;
using Chopsticks.Dependencies.Containers;
using Chopsticks.Dependencies.Resolutions;
using System;
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
        public TNativeContainer Container { get; protected set; }

        ///<inheritdoc/>
        public ContainerSetting ContainerSetting { get; protected set; }

        [SerializeField]
        private BaseUnityContainer<TNativeContainer> _overrideContainer;


        ///<inheritdoc/>
        public virtual void OnEnable()
        {
            GetUpdatedContainer(out var updatedContainer);
            Container = updatedContainer;
        }

        ///<inheritdoc/>
        public virtual void OnTransformParentChanged()
        {
            if (!enabled)
                return;

            if (!GetUpdatedContainer(out var updatedContainer))
                return;

            Container = updatedContainer;
            OnContainerChanged();
        }

        /// <summary>
        /// Invoked when the container is changed as the result of a change in the 
        /// MonoBehaviour's parent hierarchy.
        /// </summary>
        protected virtual void OnContainerChanged() { }


        protected bool GetUpdatedContainer(out TNativeContainer updatedContainer)
        {
            updatedContainer = this.FindContainer(transform, _overrideContainer);
            // TODO :: Handle None.

            return !Container.Equals(updatedContainer);
        }


        // TODO :: Wrappers to Resolve dependencies through the interface extensions.
    }
}
