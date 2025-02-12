using Chopsticks.Dependencies.Containers;

namespace MonoContainerTests.Mocks
{
    public class MockMonoContainer : 
        BaseMonoContainer<MockDependencyContainer, MockDependencyContainerFactory, 
            MockDependencyContainer.Definition, MockMonoContainerService>
    {
        public new MockMonoContainerService ContainerService => base.ContainerService;

        public new MockDependencyContainer InternalContainer => base.InternalContainer;

        public bool HasRegisteredNativeDependencies { get; set; }

        protected override void RegisterNativeDependencies() => 
            HasRegisteredNativeDependencies = true;
    }
}