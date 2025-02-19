using Chopsticks.Dependencies;
using MonoContainerTests.Mocks;

namespace IUnityDependentExtensionsTests
{
    public class MockMonoDependent : 
        BaseMonoDependent<MockDependencyContainer, MockMonoContainerService>
    {

    }
}