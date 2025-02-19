using Chopsticks.Dependencies;
using MonoContainerTests.Mocks;

namespace IUnityDependencyExtensionsTests.Mocks
{
    public class MockMonoDependency :
        BaseMonoDependency<MockDependencyContainer, MockMonoContainerService>
    {
        protected override void OnRegistration() { }
    }
}