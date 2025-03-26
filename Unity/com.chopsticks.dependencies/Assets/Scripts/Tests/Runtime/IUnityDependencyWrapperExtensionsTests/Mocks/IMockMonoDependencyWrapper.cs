using Chopsticks.Dependencies.Contained;
using MonoContainerTests.Mocks;

namespace IUnityDependencyWrapperExtensionsTests.Mocks
{
    public interface IMockMonoDependencyWrapper : 
        IUnityDependencyWrapper<MockDependencyContainer, MockMonoContainerService>
    { }
}