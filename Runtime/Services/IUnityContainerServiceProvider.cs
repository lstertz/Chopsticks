using Chopsticks.Dependencies.Containers;
using Chopsticks.Dependencies.Resolutions;
using System;

namespace Chopsticks.Dependencies.Services
{
    /// <summary>
    /// Defines a <see cref="IUnityContainerService{TNativeContainer}"/> provider.
    /// </summary>
    /// <typeparam name="TNativeContainer">The type of the internal, non-Unity 
    /// dependency container that is consumed by the Unity contianer.</typeparam>
    /// <typeparam name="TUnityContainerService">The type of service provider 
    /// for working with Unity containers.</typeparam>
    public interface IUnityContainerServiceProvider<TNativeContainer, TUnityContainerService>
        where TNativeContainer : IDependencyContainer, IDependencyResolutionProvider, IDisposable
        where TUnityContainerService : IUnityContainerService<TNativeContainer>, new()
    {
        /// <summary>
        /// The provided Unity container service.
        /// </summary>
        TUnityContainerService Service => InternalService;
        protected static TUnityContainerService InternalService { get; } = new();
    }
}
