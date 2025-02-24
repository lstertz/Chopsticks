using Chopsticks.Dependencies;
using MonoContainerTests.Mocks;

namespace IUnityDependencyExtensionsTests.Mocks
{
    public interface IMockMonoDependency { }

    public class MockMonoDependency :
        BaseMonoDependency<MockDependencyContainer, MockMonoContainerService>,
        IMockMonoDependency
    {
        protected override void OnRegistration() { }
    }
}