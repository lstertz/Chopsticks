using Chopsticks.Dependencies.Containers;
using Chopsticks.Dependencies.Resolutions;
using System;
using System.Collections.Generic;

namespace Chopsticks.Dependencies.Contained
{
    /// <summary>
    /// Provides extensions to Unity constructs that are contained within a container.
    /// </summary>
    public static class IUnityContainedExtensions
    {
        /// <inheritdoc cref="IDependencyContainerExtensions
        /// .AssertiveResolve{TContract}(IDependencyContainer, string?)"/>
        public static TContract AssertiveResolve<TNativeContainer, TContract>(
            this IUnityContained<TNativeContainer> contained, string customErrorMessage = "")
            where TNativeContainer : IDependencyContainer, IDependencyResolutionProvider, IDisposable => 
            contained.Container.AssertiveResolve<TContract>(customErrorMessage);

        /// <inheritdoc cref="IDependencyContainerExtensions
        /// .Resolve{TContract}(IDependencyContainer, out TContract)"/>
        public static bool Resolve<TNativeContainer, TContract>(
            this IUnityContained<TNativeContainer> contained,
            out TContract implementation)
            where TNativeContainer : IDependencyContainer, IDependencyResolutionProvider, IDisposable => 
            contained.Container.Resolve(out implementation);

        /// <inheritdoc cref="IDependencyContainerExtensions
        /// .ResolveAll{TContract}(IDependencyContainer)"/>
        public static IEnumerable<TContract> ResolveAll<TNativeContainer, TContract>(
            this IUnityContained<TNativeContainer> contained)
            where TNativeContainer : IDependencyContainer, IDependencyResolutionProvider, IDisposable =>
            contained.Container.ResolveAll<TContract>();
    }
}
