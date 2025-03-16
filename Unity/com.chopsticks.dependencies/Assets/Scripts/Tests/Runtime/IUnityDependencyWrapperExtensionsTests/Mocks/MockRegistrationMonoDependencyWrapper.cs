using Chopsticks.Dependencies.Contained;

namespace IUnityDependencyWrapperExtensionsTests.Mocks
{
    public interface IMockRegistrationMonoDependencyWrapper :
        IUnityDependencyWrapper<MockRegistrationDependencyContainer, MockRegistrationContainerService>
    { }
}