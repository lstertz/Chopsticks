using Chopsticks.Dependencies.Resolutions;
using Chopsticks.Dependencies.Services;
using System;

namespace Chopsticks.Dependencies.Containers
{
    /// <summary>
    /// Defines a Unity container consumer that is serviced 
    /// by <see cref="IUnityContainerService{TNativeContainer}"/>.
    /// </summary>
    /// <typeparam name="TNativeContainer">The type of the internal, non-Unity 
    /// dependency container that is consumed by the Unity contianer.</typeparam>
    /// <typeparam name="TUnityContainerService">The type of service provider 
    /// for working with Unity containers.</typeparam>
    public interface IUnityContainerConsumer<TNativeContainer, TUnityContainerService>
        where TNativeContainer : IDependencyContainer, IDependencyResolutionProvider, IDisposable
        where TUnityContainerService : IUnityContainerService<TNativeContainer>, new()
    {
        /// <summary>
        /// The Unity service that provides container services for Unity container consumers.
        /// </summary>
        TUnityContainerService Service => _unityContainerService;
        private static readonly TUnityContainerService _unityContainerService = new();
    }
}
