using Chopsticks.Dependencies.Containers;
using Chopsticks.Dependencies.Factories;
using Chopsticks.Dependencies.Services;

namespace Chopsticks.Dependencies.Contained
{
    /// <summary>
    /// Defines a Unity consumer of dependencies that is itself not a dependency.
    /// </summary>
    public interface IMonoDependent : IUnityDependent<DependencyContainer, MonoContainerService> 
    { }
}
