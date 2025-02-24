using Chopsticks.Dependencies;

namespace IUnityDependencyExtensionsTests.Mocks
{
    public class MockRegistrationMonoDependency :
        BaseMonoDependency<MockRegistrationDependencyContainer, MockRegistrationContainerService>,
        IMockMonoDependency
    {
        protected override void OnRegistration() { }
    }
}