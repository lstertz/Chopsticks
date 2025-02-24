using Chopsticks.Dependencies.Contained;
using Chopsticks.Dependencies.Containers;
using MonoContainerTests.Mocks;
using System.Collections.Generic;

namespace IUnityDependencyExtensionsTests.Mocks
{
    public abstract class MockUnityDependency :
        IUnityDependency<MockDependencyContainer, MockMonoContainerService>
    {
        public abstract MockDependencyContainer Container { get; set; }
        public abstract ContainerSetting ContainerSetting { get; }

        public abstract List<DependencyRegistration> Registrations { get; }


        public abstract void OnDisable();
        public abstract void OnEnable();
        public abstract void OnTransformParentChanged();
    }
}