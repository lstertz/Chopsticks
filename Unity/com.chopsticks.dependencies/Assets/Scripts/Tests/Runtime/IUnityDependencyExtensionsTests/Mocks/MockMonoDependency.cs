using Chopsticks.Dependencies;
using Chopsticks.Dependencies.Contained;
using MonoContainerTests.Mocks;

namespace IUnityDependencyExtensionsTests.Mocks
{
    public interface ITestContract { }

    public interface IMockMonoDependency : 
        IUnityDependency<MockDependencyContainer, MockMonoContainerService>, ITestContract
    { }

    public class MockMonoDependency :
        BaseMonoDependency<MockDependencyContainer, MockMonoContainerService>,
        IMockMonoDependency
    {
        protected override void OnRegistration() { }
    }
}