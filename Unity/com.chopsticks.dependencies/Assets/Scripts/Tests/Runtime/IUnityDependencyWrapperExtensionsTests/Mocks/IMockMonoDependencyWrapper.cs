using Chopsticks.Dependencies.Contained;
using MonoContainerTests.Mocks;

namespace IUnityDependencyWrapperExtensionsTests.Mocks
{
    public interface ITestContract { }

    public interface IMockMonoDependencyWrapper : 
        IUnityDependency<MockDependencyContainer, MockMonoContainerService>, ITestContract
    { }
}