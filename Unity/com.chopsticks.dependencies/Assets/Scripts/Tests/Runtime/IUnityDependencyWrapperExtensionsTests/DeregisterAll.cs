using Chopsticks.Dependencies.Contained;
using Chopsticks.Dependencies.Containers;
using IUnityDependencyWrapperExtensionsTests.Mocks;
using MonoContainerTests.Mocks;
using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;

namespace IUnityDependencyWrapperExtensionsTests
{
    public class DeregisterAll
    {
        [Test]
        public void DeregisterAll_WithoutRegistrations_DoesNothing()
        {
            // Set up
            var unityDependency = Substitute.For<IMockMonoDependencyWrapper>();
            unityDependency.Registrations.Returns(new List<DependencyRegistration>());
            unityDependency.Container = Substitute.For<MockDependencyContainer>();

            // Act
            unityDependency.DeregisterAll();

            // Assert
            unityDependency.Container.DidNotReceiveWithAnyArgs().Deregister(
                Arg.Any<DependencyRegistration>());
        }

        [Test]
        public void DeregisterAll_WithRegistrations_ClearsRegistrations()
        {
            // Set up
            DependencyRegistration registrationA = new()
            {
                Contract = typeof(object)
            };
            DependencyRegistration registrationB = new()
            {
                Contract = typeof(object)
            };
            List<DependencyRegistration> registrations = new()
            {
                registrationA,
                registrationB
            };

            var unityDependency = Substitute.For<IMockMonoDependencyWrapper>();
            unityDependency.Container = Substitute.For<MockDependencyContainer>();
            unityDependency.Registrations.Returns(registrations);

            // Act
            unityDependency.DeregisterAll();

            // Assert
            Assert.That(registrations, Is.Empty);
        }

        [Test]
        public void DeregisterAll_WithRegistrations_DeregistersAll()
        {
            // Set up
            DependencyRegistration registrationA = new()
            {
                Contract = typeof(object)
            };
            DependencyRegistration registrationB = new()
            {
                Contract = typeof(object)
            };
            List<DependencyRegistration> registrations = new()
            {
                registrationA,
                registrationB
            };

            var unityDependency = Substitute.For<IMockMonoDependencyWrapper>();
            unityDependency.Container = Substitute.For<MockDependencyContainer>();
            unityDependency.Registrations.Returns(registrations);

            // Act
            unityDependency.DeregisterAll();

            // Assert
            unityDependency.Container.Received().Deregister(registrationA);
            unityDependency.Container.Received().Deregister(registrationB);
        }
    }
}