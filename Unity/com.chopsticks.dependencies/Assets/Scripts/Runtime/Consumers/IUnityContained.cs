using Chopsticks.Dependencies.Containers;
using Chopsticks.Dependencies.Resolutions;
using System;

namespace Chopsticks.Dependencies.Consumers
{
    /// <summary>
    /// Defines a Unity construct that is contained by 
    /// a <see cref="IUnityContainer{TNativeContainer}"/>.
    /// </summary>
    /// <typeparam name="TNativeContainer">The type of the internal, non-Unity 
    /// dependency container of this construct.</typeparam>
    public interface IUnityContained<TNativeContainer>
        where TNativeContainer : IDependencyContainer, IDependencyResolutionProvider, IDisposable
    {
        /// <summary>
        /// The container of this contained construct.
        /// </summary>
        TNativeContainer Container { get; }

        /// <summary>
        /// The setting that specifies how the <see cref="Container"/> is found.
        /// </summary>
        ContainerSetting ContainerSetting { get; }
    }


    /// <summary>
    /// Defines a Unity construct that is contained by 
    /// a <see cref="IUnityContainer{TNativeContainer}"/> with container services 
    /// provided by <see cref="IUnityContainerService{TNativeContainer}"/>.
    /// </summary>
    /// <typeparam name="TNativeContainer">The type of the internal, non-Unity 
    /// dependency container of this construct.</typeparam>
    /// <typeparam name="TUnityContainerService">The type of service provider 
    /// for working with Unity containers.</typeparam>
    public interface IUnityContained<TNativeContainer, TUnityContainerService> :
        IUnityContained<TNativeContainer>
        where TNativeContainer : IDependencyContainer, IDependencyResolutionProvider, IDisposable
        where TUnityContainerService : IUnityContainerService<TNativeContainer>, new()
    {
        /// <summary>
        /// The Unity service that provides container services for Unity dependents.
        /// </summary>
        public static TUnityContainerService UnityContainerService { get; } = new();
    }
}
