namespace Chopsticks.Dependencies.Containers
{
    /// <summary>
    /// The setting to define what native container is retrieved 
    /// from an <see cref="IUnityContainerService{TNativeContainer, TUnityContainer}"/>.
    /// </summary>
    public enum ContainerRetrievalSetting
    {
        /// <summary>
        /// The retrieved container will be found through the hierarchy, 
        /// defaulting to the global container if no container can be found.
        /// </summary>
        Hierarchy = 0,
        /// <summary>
        /// The retrieved container will be the global container.
        /// </summary>
        Global = 1,
        /// <summary>
        /// The retrieved container will be a specified override container.
        /// </summary>
        Override = 2
    }
}
