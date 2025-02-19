using Chopsticks.Dependencies.Contained;
using Chopsticks.Dependencies.Containers;
using Chopsticks.Dependencies.Resolutions;
using Chopsticks.Dependencies.Services;
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
        public override void OnEnable() =>
            this.SetContainer(this, OverrideContainer, OnRegistration);

        ///<inheritdoc/>
        public virtual void OnDisable() => 
            this.Deregister();

        ///<inheritdoc/>
        public override void OnTransformParentChanged() => 
            this.UpdateContainer(this, OverrideContainer, 
                OnPreContainerChanged, OnPostContainerChanged, OnRegistration);


        /// <summary>
        /// Performs registration of this dependency for each of its contracts 
        /// using <see cref="RegisterAs{T}"/>.
        /// </summary>
        protected abstract void OnRegistration();

        /// <summary>
        /// Registers this dependency as the specified contract.
        /// </summary>
        /// <typeparam name="TContract">The contract to be registered as.</typeparam>
        protected void RegisterAs<TContract>() =>
            this.RegisterAs<TNativeContainer, TUnityContainerService, TContract>();
    }
}
