using Chopsticks.Dependencies.Contained;

namespace IUnityDependencyExtensionsTests.Mocks
{
    public interface IMockRegistrationMonoDependency :
        IUnityDependency<MockRegistrationDependencyContainer, MockRegistrationContainerService>,
        ITestContract
    { }
}