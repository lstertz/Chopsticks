using Chopsticks.Dependencies.Containers;
using Chopsticks.Dependencies.Factories;

namespace Chopsticks.Dependencies.Services
{
    /// <summary>
    /// Provides services for working with <see cref="MonoContainer"/>s, including 
    /// access to a global instance and strategies to work with other containers.
    /// </summary>
    public class MonoContainerService : UnityContainerService<
        DependencyContainer, DefaultDependencyContainerFactory, DependencyContainerDefinition>
    {
        /// <summary>
        /// A static, global instance of a container, for use as a default or 
        /// the highest-level container of a hierarchy of containers.
        /// </summary>
        public static DependencyContainer GlobalContainer => _globalContainer;


        /// <summary>
        /// Resets the <see cref="GlobalContainer"/>.
        /// </summary>
        /// <param name="definition">The optional definition to specify the 
        /// settings of the container.</param>
        public static void ResetGlobal(DependencyContainerDefinition definition = default)
        {
            _globalContainer?.Dispose();
            _globalContainer = _instanceFactory.BuildContainer(definition);
        }
    }
}
