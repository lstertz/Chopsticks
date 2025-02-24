namespace Chopsticks.Dependencies.Containers
{
    /// <summary>
    /// The setting to define the encapsulating container of another container 
    /// or a dependent.
    /// </summary>
    public enum ContainerSetting
    {
        /// <summary>
        /// The contained will have no container.
        /// </summary>
        None = -1,
        /// <summary>
        /// The contained will find its parent through the hierarchy, 
        /// defaulting to the global container if no parent can be found.
        /// </summary>
        HierarchyWithGlobal = ContainerRetrievalSetting.HierarchyWithGlobal,
        /// <summary>
        /// The contained will find its parent through the hierarchy and 
        /// will have no parent if one cannot be found.
        /// </summary>
        HierarchyWithoutGlobal = ContainerRetrievalSetting.HierarchyWithoutGlobal,
        /// <summary>
        /// The contained will be a child of the global container.
        /// </summary>
        Global = ContainerRetrievalSetting.Global,
        /// <summary>
        /// A specified override container will be this contained's container.
        /// </summary>
        Override = ContainerRetrievalSetting.Override
    }
}
