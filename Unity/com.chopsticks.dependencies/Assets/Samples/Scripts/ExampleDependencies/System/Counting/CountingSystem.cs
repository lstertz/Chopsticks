using Chopsticks.Dependencies.Contained;
using Chopsticks.Dependencies.Containers;
using Chopsticks.Dependencies.Services;
using System.Collections.Generic;


namespace Chopsticks.Samples.ExampleDependencies.System.Counting
{
    public class CountingSystem : UnalterableCountingSystemSuperclass, IMonoDependency
    {
        public List<DependencyRegistration> Registrations { get; } = new();

        public DependencyContainer Container { get; set; }

        public ContainerSetting ContainerSetting => ContainerSetting.HierarchyWithGlobal;


        public void OnDisable() =>
            this.DeregisterAll();

        public void OnEnable() =>
            this.SetContainer(this, null, OnRegistration);

        public void OnTransformParentChanged() =>
            this.UpdateContainer(this, null, null, null);

        protected void OnRegistration()
        {
            this.RegisterAs<DependencyContainer, MonoContainerService, ISystem>();
        }
    }
}
