using Chopsticks.Dependencies.Containers;
using Chopsticks.Dependencies.Resolutions;
using System;

namespace Chopsticks.Dependencies.Consumers
{
    /// <summary>
    /// Provides extensions to Unity dependents.
    /// </summary>
    public static class IUnityDependentExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TNativeContainer"></typeparam>
        /// <typeparam name="TNativeContainerDefinition"></typeparam>
        /// <typeparam name="TUnityContainer"></typeparam>
        /// <typeparam name="TUnityContainerService"></typeparam>
        /// <param name="dependent"></param>
        /// <returns></returns>
        public static TUnityContainer FindContainer<TNativeContainer, 
            TNativeContainerDefinition, TUnityContainer, TUnityContainerService>(
            this IUnityDependent<TNativeContainer, TNativeContainerDefinition, 
                TUnityContainer, TUnityContainerService> dependent)
        where TNativeContainer : IDependencyContainer, IDependencyResolutionProvider, IDisposable
        where TUnityContainer : IDependencyContainer
        where TUnityContainerService : IUnityContainerService<TNativeContainer,
            TNativeContainerDefinition>, new()
        {
            var service = IUnityDependent<TNativeContainer, TNativeContainerDefinition,
                TUnityContainer, TUnityContainerService>.UnityContainerService;

            // TODO :: Implement.
            return default;
        }

        // TODO :: Resolve extension to get dependencies.
    }
}
