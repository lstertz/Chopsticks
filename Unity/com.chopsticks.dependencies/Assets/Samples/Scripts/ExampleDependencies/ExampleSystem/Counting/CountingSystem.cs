using Chopsticks.Dependencies.Contained;
using Chopsticks.Dependencies.Containers;
using Chopsticks.Dependencies.Services;
using System.Collections.Generic;


namespace Chopsticks.Samples.ExampleDependencies.ExampleSystem.Counting
{
    /// <summary>
    /// An example class that shows how an unalterable (third-party or Unity native) MonoBehaviour 
    /// can be augmented with the same features offered by inheriting from 
    /// <see cref="Dependencies.MonoDependency"/>.
    /// </summary>
    public class CountingSystem : UnalterableCountingSystemSuperclass, IMonoDependency
    {
        /// <inheritdoc/>
        List<DependencyRegistration> IUnityDependency<DependencyContainer, MonoContainerService>
            .Registrations { get; } = new();

        /// <inheritdoc/>
        DependencyContainer IUnityContained<DependencyContainer>.Container { get; set; }

        /// <inheritdoc/>
        ContainerSetting IUnityContained<DependencyContainer>.ContainerSetting => 
            ContainerSetting.HierarchyWithGlobal;


        /// <inheritdoc/>
        public void OnDisable() =>
            this.DeregisterAll();

        /// <inheritdoc/>
        public void OnEnable() =>
            this.SetContainer(this, null, PerformRegistration);

        /// <inheritdoc/>
        public void OnTransformParentChanged() =>
            this.UpdateContainer(this, null, null, null);


        /// <summary>
        /// Performs the registration of this dependency as its contracts.
        /// </summary>
        protected void PerformRegistration()
        {
            this.RegisterAs<IExampleSystem>();
        }
    }
}
