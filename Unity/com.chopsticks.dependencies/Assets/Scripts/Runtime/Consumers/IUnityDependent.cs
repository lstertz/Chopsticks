using Chopsticks.Dependencies.Containers;
using Chopsticks.Dependencies.Resolutions;
using System;

namespace Chopsticks.Dependencies.Consumers
{
    /// <summary>
    /// Defines a Unity consumer of dependencies that is itself not a dependency.
    /// </summary>
    /// <typeparam name="TNativeContainer">The type of the internal, non-Unity 
    /// dependency container.</typeparam>
    /// <typeparam name="TUnityContainer">The type of the Unity dependency container that 
    /// encapsulates the <see cref="TNativeContainer"/> and the dependent.</typeparam>
    /// <typeparam name="TNativeContainerDefinition">The type of definition to define any custom 
    /// properties of the internal, non-Unity dependency container.</typeparam>
    /// <typeparam name="TUnityContainerService">The type of the Unity container service that 
    /// provides Unity-specific services.</typeparam>
    public interface IUnityDependent<TNativeContainer, TNativeContainerDefinition,
        TUnityContainer, TUnityContainerService>
        where TNativeContainer : IDependencyContainer, IDependencyResolutionProvider, IDisposable
        where TUnityContainer : IDependencyContainer
        where TUnityContainerService : IUnityContainerService<TNativeContainer,
            TNativeContainerDefinition>, new()
    {
        /// <summary>
        /// The Unity service that provides container services for Unity dependents.
        /// </summary>
        public static TUnityContainerService UnityContainerService { get; } = new();


        /// <summary>
        /// The container of this dependent.
        /// </summary>
        TUnityContainer Container { get; }

        /// <summary>
        /// The setting that specifies how the Container is found.
        /// </summary>
        ContainerSetting ContainerSetting { get; }


        /// <summary>
        /// Performed when the Unity object is enabled.
        /// This ensures the contained has its appropriate container, 
        /// and is registered, if appropriate.
        /// </summary>
        void OnEnable();

        /// <summary>
        /// Performed when the Unity transform hierarchy changes.
        /// This ensures the contained has its appropriate container, 
        /// updating any registrations, if appropriate.
        /// </summary>
        void OnTransformParentChanged();
    }
}
