using Chopsticks.Dependencies.Contained;
using MonoContainerTests.Mocks;

namespace IUnityDependencyExtensionsTests.Mocks
{
    public interface ITestContract { }

    public interface IMockMonoDependency : 
        IUnityDependency<MockDependencyContainer, MockMonoContainerService>, ITestContract
    { }
}