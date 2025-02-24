using Chopsticks.Dependencies.Contained;
using Chopsticks.Dependencies.Containers;
using MonoContainerTests.Mocks;

namespace IUnityDependentExtensionsTests.Mocks
{
    public abstract class MockUnityDependent :
        IUnityDependent<MockDependencyContainer, MockMonoContainerService>
    {
        public abstract MockDependencyContainer Container { get; set; }
        public abstract ContainerSetting ContainerSetting { get; }

        public abstract void OnEnable();
        public abstract void OnTransformParentChanged();
    }
}