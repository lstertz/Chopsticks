using Chopsticks.Dependencies.Containers;
using Chopsticks.Dependencies.Resolutions;
using Chopsticks.Dependencies.Services;
using System;

namespace Chopsticks.Dependencies.Contained
{
    /// <summary>
    /// Provides extensions to Unity dependency wrappers.
    /// </summary>
    public static class IUnityDependencyWrapperExtensions
    {
        /// <summary>
        /// Deregisters all of this dependency wrapper's registrations with its container.
        /// </summary>
        /// <typeparam name="TNativeContainer">The type of the internal, non-Unity 
        /// dependency container.</typeparam>
        /// <typeparam name="TUnityContainerService">The type of the Unity container service that 
        /// provides Unity-specific services.</typeparam>
        /// <param name="wrapper">This dependency wrapper that is having its registrations 
        /// deregistered.</param>
        public static void DeregisterAll<TNativeContainer, TUnityContainerService>(
            this IUnityDependencyWrapper<TNativeContainer, TUnityContainerService> wrapper)
            where TNativeContainer : IDependencyContainer, IDependencyResolutionProvider, IDisposable
            where TUnityContainerService : IUnityContainerService<TNativeContainer>, new()
        {
            foreach (var registration in wrapper.Registrations)
                wrapper.Container.Deregister(registration);
            wrapper.Registrations.Clear();
        }


        /// <summary>
        /// Registers the specified dependency as the specified contract for this 
        /// wrapper's container.
        /// </summary>
        /// <typeparam name="TNativeContainer">The type of the internal, non-Unity 
        /// dependency container.</typeparam>
        /// <typeparam name="TUnityContainerService">The type of the Unity container service that 
        /// provides Unity-specific services.</typeparam>
        /// <typeparam name="TContract">The contract to be registered as.</typeparam>
        /// <param name="dependency">The dependency instance to be registered.</param>
        /// <returns>The registration, to be used for manual deregistration, if needed.
        /// This will be null if the attempt to register failed.</returns>
        public static DependencyRegistration Register<TNativeContainer, 
            TUnityContainerService, TContract>(
                this IUnityDependencyWrapper<TNativeContainer, TUnityContainerService> wrapper,
                TContract dependency)
            where TNativeContainer : IDependencyContainer, IDependencyResolutionProvider, IDisposable
            where TUnityContainerService : IUnityContainerService<TNativeContainer>, new()
        {
            wrapper.Container.Register(new DependencySpecification()
            {
                Contract = typeof(TContract),
                ImplementationFactory = c => dependency,
                Lifetime = DependencyLifetime.Singleton,
            }, out var registration);
            wrapper.Registrations.Add(registration);

            return registration;
        }

        /// <summary>
        /// Registers the specified implementation factory to fulfill the specified contract 
        /// for this wrapper's container.
        /// </summary>
        /// <typeparam name="TContract">The contract to be registered as.</typeparam>
        /// <param name="implementationFactory">The factory that will be 
        /// registered to produce implementations that fulfill the contract as 
        /// dependencies.</param>
        /// <param name="lifetime">The lifetime that the registered 
        /// dependency will have.</param>
        /// <returns>The registration, to be used for manual deregistration, if needed.
        /// This will be null if the attempt to register failed.</returns>
        public static DependencyRegistration Register<TNativeContainer,
            TUnityContainerService, TContract>(
                this IUnityDependencyWrapper<TNativeContainer, TUnityContainerService> wrapper,
                Func<IDependencyContainer, TContract> implementationFactory,
                DependencyLifetime lifetime = DependencyLifetime.Singleton)
            where TNativeContainer : IDependencyContainer, IDependencyResolutionProvider, IDisposable
            where TUnityContainerService : IUnityContainerService<TNativeContainer>, new()
        {
            wrapper.Container.Register(new DependencySpecification()
            {
                Contract = typeof(TContract),
                ImplementationFactory = c => implementationFactory(c),
                Lifetime = lifetime,
            }, out var registration);
            wrapper.Registrations.Add(registration);

            return registration;
        }
    }
}
