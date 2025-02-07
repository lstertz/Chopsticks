using Chopsticks.Dependencies.Containers;
using Chopsticks.Dependencies.Resolutions;
using System;

namespace Chopsticks.Dependencies.Consumers
{
    /// <summary>
    /// Defines a Unity consumer of dependencies that is itself a dependency.
    /// </summary>
    /// <typeparam name="TNativeContainer">The type of the internal, non-Unity 
    /// dependency container.</typeparam>
    /// <typeparam name="TUnityContainer">The type of the Unity dependency container that 
    /// encapsulates the <see cref="TNativeContainer"/> and the dependency.</typeparam>
    /// <typeparam name="TNativeContainerDefinition">The type of definition to define any custom 
    /// properties of the internal, non-Unity dependency container.</typeparam>
    /// <typeparam name="TUnityContainerService">The type of the Unity container service that 
    /// provides Unity-specific services.</typeparam>
    public interface IUnityDependency<TNativeContainer, TNativeContainerDefinition,
        TUnityContainer, TUnityContainerService> : IUnityDependent<TNativeContainer, 
            TNativeContainerDefinition, TUnityContainer, TUnityContainerService>
        where TNativeContainer : IDependencyContainer, IDependencyResolutionProvider, IDisposable
        where TUnityContainer : IDependencyContainer
        where TUnityContainerService : IUnityContainerService<TNativeContainer,
            TNativeContainerDefinition>, new()
    {
        /// <summary>
        /// Performed when the Unity object is disabled.
        /// This performs any deregistration that may be appropriate.
        /// </summary>
        void OnDisable();
    }
}
