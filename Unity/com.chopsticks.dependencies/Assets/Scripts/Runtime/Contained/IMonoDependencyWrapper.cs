using Chopsticks.Dependencies.Containers;
using Chopsticks.Dependencies.Services;

namespace Chopsticks.Dependencies.Contained
{
    /// <summary>
    /// Defines a Unity consumer of dependencies that is a wrapper for 
    /// a non-Unity dependency.
    /// </summary>
    public interface IMonoDependencyWrapper :
        IUnityDependencyWrapper<DependencyContainer, MonoContainerService>
    { }
}
