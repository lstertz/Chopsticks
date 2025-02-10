using Chopsticks.Dependencies.Contained;
using Chopsticks.Dependencies.Containers;
using Chopsticks.Dependencies.Resolutions;
using System;

namespace Chopsticks.Dependencies
{
    ///<inheritdoc cref="IUnityDependency{TNativeContainer, TUnityContainerService}"/>
    public abstract class BaseMonoDependency<TNativeContainer, TUnityContainerService> : 
        BaseMonoDependent<TNativeContainer, TUnityContainerService>, 
        IUnityDependency<TNativeContainer, TUnityContainerService>
        where TNativeContainer : IDependencyContainer, IDependencyResolutionProvider, IDisposable
        where TUnityContainerService : IUnityContainerService<TNativeContainer>, new()
    {
        ///<inheritdoc/>
        public override void OnEnable()
        {
            base.OnEnable();
            OnRegistration();
        }

        ///<inheritdoc/>
        public virtual void OnDisable() => Deregister();

        ///<inheritdoc/>
        public override void OnTransformParentChanged()
        {
            if (!enabled)
                return;

            if (!GetUpdatedContainer(out var updatedContainer))
                return;

            Deregister();
            Container = updatedContainer;
            OnRegistration();

            OnContainerChanged();
        }


        /// <summary>
        /// Performs registration of this dependency for each of its contracts 
        /// using <see cref="RegisterAs{T}"/>.
        /// </summary>
        protected abstract void OnRegistration();

        protected void RegisterAs<T>()
        {
            // TODO :: Uses interface extension method to register.
            // TODO :: Tracks registrations.
        }


        private void Deregister()
        {
            // TODO :: Deregisters all tracked registrations.
        }
    }
}
