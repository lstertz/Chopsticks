namespace Chopsticks.Dependencies.Containers
{
    /// <summary>
    /// The setting to define how a container finds its parent.
    /// </summary>
    public enum ContainerParentSetting
    {
        /// <summary>
        /// The container will have no parent.
        /// </summary>
        None = -1,
        /// <summary>
        /// The container will find its parent through the hierarchy, 
        /// defaulting to the global container if no parent can be found.
        /// </summary>
        HierarchyWithGlobal = ContainerRetrievalSetting.HierarchyWithGlobal,
        /// <summary>
        /// The container will find its parent through the hierarchy and 
        /// will have no parent if one cannot be found.
        /// </summary>
        HierarchyWithoutGlobal = ContainerRetrievalSetting.HierarchyWithoutGlobal,
        /// <summary>
        /// The container will be a child of the global container.
        /// </summary>
        Global = ContainerRetrievalSetting.Global,
        /// <summary>
        /// A specified override parent will be this container's parent.
        /// </summary>
        Override = ContainerRetrievalSetting.Override
    }
}
