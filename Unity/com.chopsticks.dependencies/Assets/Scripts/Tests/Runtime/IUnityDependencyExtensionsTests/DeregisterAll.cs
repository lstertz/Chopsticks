using Chopsticks.Dependencies.Contained;
using Chopsticks.Dependencies.Containers;
using MonoContainerTests.Mocks;
using NSubstitute;
using NUnit.Framework;

namespace IUnityDependencyExtensionsTests
{
    public class DeregisterAll
    {
        [Test]
        public void DeregisterAll_WithRegistrations_DeregistersAll()
        {
            // Set up
            var unityDependency = Substitute.For<IUnityDependency<MockDependencyContainer, 
                MockMonoContainerService>>();

            DependencyRegistration registrationA = new()
            {
                Contract = typeof(object)
            };
            DependencyRegistration registrationB = new()
            {
                Contract = typeof(object)
            };
            unityDependency.Registrations.Add(registrationA);
            unityDependency.Registrations.Add(registrationB);

            // Act
            unityDependency.DeregisterAll();

            // Assert
            Assert.That(unityDependency.Registrations, Is.Empty);
            unityDependency.Container.Received().Deregister(registrationA);
            unityDependency.Container.Received().Deregister(registrationB);
        }

        [Test]
        public void DeregisterAll_WithoutRegistrations_DoesNothing()
        {
            // Set up
            var unityDependency = Substitute.For<IUnityDependency<MockDependencyContainer,
                MockMonoContainerService>>();

            // Act
            unityDependency.DeregisterAll();

            // Assert
            Assert.That(unityDependency.Registrations, Is.Empty);
            unityDependency.Container.DidNotReceiveWithAnyArgs().Deregister(
                Arg.Any<DependencyRegistration>());
        }
    }
}