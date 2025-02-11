using Chopsticks.Dependencies.Containers;
using Chopsticks.Dependencies.Resolutions;
using System;

namespace Chopsticks.Dependencies.Contained
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
        TNativeContainer Container { get; set; }

        /// <summary>
        /// The setting that specifies how the <see cref="Container"/> is found.
        /// </summary>
        ContainerSetting ContainerSetting { get; }
    }
}
