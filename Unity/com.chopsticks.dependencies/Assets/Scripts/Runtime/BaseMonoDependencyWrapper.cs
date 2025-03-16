using Chopsticks.Dependencies.Contained;
using Chopsticks.Dependencies.Containers;
using Chopsticks.Dependencies.Resolutions;
using Chopsticks.Dependencies.Services;
using System;
using System.Collections.Generic;

namespace Chopsticks.Dependencies
{
    ///<inheritdoc cref="IUnityDependencyWrapper{TNativeContainer, TUnityContainerService}"/>
    public abstract class BaseMonoDependencyWrapper<TNativeContainer, TUnityContainerService> :
        BaseMonoDependent<TNativeContainer, TUnityContainerService>,
        IUnityDependencyWrapper<TNativeContainer, TUnityContainerService>
        where TNativeContainer : IDependencyContainer, IDependencyResolutionProvider, IDisposable
        where TUnityContainerService : IUnityContainerService<TNativeContainer>, new()
    {
        ///<inheritdoc/>
        List<DependencyRegistration> IUnityDependencyWrapper<TNativeContainer, TUnityContainerService>
            .Registrations => _registrations;
        private readonly List<DependencyRegistration> _registrations = new(1);


        ///<inheritdoc/>
        public override void OnEnable()
        {
            this.UpdateContainer(this, OverrideContainer);
            OnContainerSet();
        }

        ///<inheritdoc/>
        public virtual void OnDisable() =>
            this.DeregisterAll();

        ///<inheritdoc/>
        public override void OnTransformParentChanged() =>
            this.UpdateContainer(this, OverrideContainer, this.DeregisterAll, OnContainerSet);


        /// <summary>
        /// Performs registration of this dependency for each of its contracts 
        /// using <see cref="RegisterAs{T}"/>.
        /// </summary>
        protected abstract void PerformRegistration();


        /// <summary>
        /// Registers the specified dependency as the specified contract.
        /// </summary>
        /// <remarks>
        /// This should be used to enforce compile time type safety when registering 
        /// a dependency, or when registering a dependency that is not a 
        /// <see cref="IUnityDependency{TNativeContainer, TUnityContainerService}"/>, 
        /// such as a non-Unity implementation.
        /// </remarks>
        /// <typeparam name="TContract">The contract to be registered as.</typeparam>
        /// <param name="dependency">The dependency instance to be registered.</param>
        /// <returns>The registration, to be used for manual deregistration, if needed.
        /// This will be null if the attempt to register failed.</returns>
        protected DependencyRegistration Register<TContract>(TContract dependency) =>
            this.Register<TNativeContainer, TUnityContainerService, TContract>(dependency);


        /// <summary>
        /// Registers the specified implementation factory to fulfill the specified contract.
        /// </summary>
        /// <remarks>
        /// This should be used when (likely non-Unity) dependencies that do not 
        /// currently exist as explicit instances need to be registered.
        /// </remarks>
        /// <typeparam name="TContract">The contract to be registered as.</typeparam>
        /// <param name="implementationFactory">The factory that will be 
        /// registered to produce implementations that fulfill the contract as 
        /// dependencies.</param>
        /// <param name="lifetime">The lifetime that the registered 
        /// dependency will have.</param>
        /// <returns>The registration, to be used for manual deregistration, if needed.
        /// This will be null if the attempt to register failed.</returns>
        protected DependencyRegistration Register<TContract>(
            Func<IDependencyContainer, TContract> implementationFactory,
            DependencyLifetime lifetime = DependencyLifetime.Singleton) =>
            this.Register<TNativeContainer, TUnityContainerService, TContract>(
                implementationFactory, lifetime);


        private void OnContainerSet()
        {
            PerformRegistration();
            ResolveDependencies();
        }
    }
}
