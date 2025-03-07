using Chopsticks.Dependencies.Exceptions;
using System;
using System.Collections.Generic;

namespace Chopsticks.Dependencies.Containers
{
    /// <summary>
    /// Extensions for <see cref="IDependencyContainer"/>.
    /// </summary>
    public static class IDependencyContainerExtensions
    {
        private const string DefaultErrorMessage = "The requested dependency of type " +
            "{0} could not be resolved or resolved with null.";


        /// <summary>
        /// Registers the dependency defined by the given specification.
        /// </summary>
        /// <param name="container">The container registering the dependency.</param>
        /// <param name="specification">The specification that defines the 
        /// dependency to be registered.</param>
        /// <returns>This container, to chain additional manipulations.</returns>
        public static IDependencyContainer Register(
            this IDependencyContainer container,
            DependencySpecification specification) =>
                container.Register(specification, out _);

        /// <summary>
        /// Registers the dependency defined by the given instance of the dependency, 
        /// with a lifetime of <see cref="DependencyLifetime.Singleton"/>.
        /// </summary>
        /// <param name="container">The container registering the dependency.</param>
        /// <param name="dependency">The instance of the dependency.</param>
        /// <returns>This container, to chain additional manipulations.</returns>
        public static IDependencyContainer Register<TContract>(
            this IDependencyContainer container,
            TContract dependency) =>
                container.Register(new DependencySpecification()
                {
                    Contract = typeof(TContract),
                    ImplementationFactory = c => dependency,
                    Lifetime = DependencyLifetime.Singleton,
                }, out _);

        /// <summary>
        /// Registers the dependency defined by the given instance of the dependency, 
        /// with a lifetime of <see cref="DependencyLifetime.Singleton"/>.
        /// </summary>
        /// <param name="container">The container registering the dependency.</param>
        /// <param name="dependency">The instance of the dependency.</param>
        /// <param name="registration">The registration that can identify the dependency for 
        /// deregistration by <see cref="IDependencyContainer.Deregister(DependencyRegistration)"/>
        /// </param>
        /// <returns>This container, to chain additional manipulations.</returns>
        public static IDependencyContainer Register<TContract>(
            this IDependencyContainer container,
            TContract dependency, 
            out DependencyRegistration registration) =>
                container.Register(new DependencySpecification()
                {
                    Contract = typeof(TContract),
                    ImplementationFactory = c => dependency,
                    Lifetime = DependencyLifetime.Singleton,
                }, out registration);

        /// <summary>
        /// Registers the dependency defined by the given implementation factory.
        /// </summary>
        /// <param name="container">The container registering the dependency.</param>
        /// <param name="implementationFactory">The factory for producing 
        /// resolving implementations.</param>
        /// <param name="lifetime">The lifetime of that the registered 
        /// dependency will have.</param>
        /// <returns>This container, to chain additional manipulations.</returns>
        public static IDependencyContainer Register<TContract>(
            this IDependencyContainer container,
            Func<IDependencyContainer, TContract> implementationFactory,
            DependencyLifetime lifetime = DependencyLifetime.Singleton) =>
                container.Register(new DependencySpecification()
                {
                    Contract = typeof(TContract),
                    ImplementationFactory = c => implementationFactory(c),
                    Lifetime = lifetime,
                }, out _);

        /// <summary>
        /// Registers the dependency defined by the given implementation factory.
        /// </summary>
        /// <param name="container">The container registering the dependency.</param>
        /// <param name="implementationFactory">The factory for producing 
        /// resolving implementations.</param>
        /// <param name="registration">The registration that can identify the dependency for 
        /// deregistration by <see cref="IDependencyContainer.Deregister(DependencyRegistration)"/>
        /// </param>
        /// <param name="lifetime">The lifetime of that the registered 
        /// dependency will have.</param>
        /// <returns>This container, to chain additional manipulations.</returns>
        public static IDependencyContainer Register<TContract>(
            this IDependencyContainer container,
            Func<IDependencyContainer, TContract> implementationFactory,
            out DependencyRegistration registration,
            DependencyLifetime lifetime = DependencyLifetime.Singleton) =>
                container.Register(new DependencySpecification()
                {
                    Contract = typeof(TContract),
                    ImplementationFactory = c => implementationFactory(c),
                    Lifetime = lifetime,
                }, out registration);


        /// <summary>
        /// Resolves the dependency of the specified type with the 
        /// first registered implementation.
        /// </summary>
        /// <typeparam name="TContract">The type of the contract is to be resolved.</typeparam>
        /// <param name="container">The container resolving the dependency.</param>
        /// <param name="customErrorMessage">The custom message of the exception 
        /// thrown if the dependency could not be resolved or was resolved as null.</param>
        /// <exception cref="MissingDependencyException">Thrown if the specified 
        /// dependency could not be resolved or was resolved as null.</exception>
        /// <returns>The resolved dependency.</returns>
        public static TContract AssertiveResolve<TContract>(
            this IDependencyContainer container, 
            string? customErrorMessage = null)
        {
            var wasResolved = container.Resolve(typeof(TContract), out var uncastImplementation);
            if (wasResolved && uncastImplementation is TContract implementation)
                return implementation;

            throw new MissingDependencyException(customErrorMessage ?? 
                string.Format(DefaultErrorMessage, typeof(TContract).FullName));
        }

        /// <summary>
        /// Resolves the dependency of the specified type with the 
        /// first registered implementation.
        /// </summary>
        /// <param name="container">The container resolving the dependency.</param>
        /// <param name="contract">The type of the contract that is to be resolved.</param>
        /// <param name="customErrorMessage">The custom message of the exception 
        /// thrown if the dependency could not be resolved.</param>
        /// <exception cref="MissingDependencyException">Thrown if the specified 
        /// dependency could not be resolved.</exception>
        /// <returns>The resolving dependency implementation.</returns>
        public static object AssertiveResolve(
            this IDependencyContainer container, 
            Type contract,
            string? customErrorMessage = null)
        {
            if (container.Resolve(contract, out var implementation) && implementation is not null)
                return implementation;

            throw new MissingDependencyException(customErrorMessage ??
                string.Format(DefaultErrorMessage, contract.FullName));
        }

        /// <summary>
        /// Resolves the dependency of the specified type with the 
        /// first registered implementation.
        /// </summary>
        /// <typeparam name="TContract">The type of the contract is to be resolved.</typeparam>
        /// <param name="container">The container resolving the dependency.</param>
        /// <param name="implementation">The resolving dependency implementation, or null 
        /// if it could not be resolved.</param>
        /// <returns>Whether the dependency was successfully resolved.</returns>
        public static bool Resolve<TContract>(
            this IDependencyContainer container,
            out TContract? implementation)
        {
            var wasResolved = container.Resolve(typeof(TContract), out var uncastImplementation);
            if (uncastImplementation is not TContract castImplementation)
            {
                implementation = default;
                return false;
            }

            implementation = castImplementation;
            return wasResolved;
        }

        /// <summary>
        /// Resolves the dependency of the specified type with all registered implementations.
        /// </summary>
        /// <typeparam name="TContract">The type of the contract is to be resolved.</typeparam>
        /// <param name="container">The container resolving the dependency.</param>
        /// <returns>The collection of all resolving implementations.</returns>
        public static IEnumerable<TContract> ResolveAll<TContract>(
            this IDependencyContainer container)
        {
            foreach (var uncastImplementation in container.ResolveAll(typeof(TContract)))
                if (uncastImplementation is TContract implementation)
                    yield return implementation;
        }
    }
}
