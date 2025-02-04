using Chopsticks.Dependencies.Resolutions;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Chopsticks.Dependencies.Containers
{
    /// <summary>
    /// Designates that all child GameObject Containers/Dependencies 
    /// are contained within this container. This enables the organization 
    /// of dependencies to be defined through the Unity hierarchy and prefabs.
    /// </summary>
    /// <typeparam name="TNativeContainer">The type of the native container that manages 
    /// the dependencies of this mono container.</typeparam>
    public abstract class BaseUnityContainer<TNativeContainer> :
        MonoBehaviour, IDependencyContainer, IUnityContainer<TNativeContainer>
        where TNativeContainer : IDependencyContainer, IDependencyResolutionProvider, IDisposable
    {
        /// <inheritdoc/>
        TNativeContainer IUnityContainer<TNativeContainer>.NativeContainer => InternalContainer;

        /// <summary>
        /// The internal, non-Unity container that manages the actual dependencies of 
        /// this Unity Container.
        /// </summary>
        protected abstract TNativeContainer InternalContainer { get; }


        /// <inheritdoc/>
        public abstract IDependencyContainer Deregister(DependencyRegistration registration);

        /// <inheritdoc/>
        public abstract IDependencyContainer Register(DependencySpecification specification, 
            out DependencyRegistration registration);

        /// <inheritdoc/>
        public abstract bool Resolve(Type contract, out object implementation);

        /// <inheritdoc/>
        public abstract IEnumerable<object> ResolveAll(Type contract);
    }
}
