using Chopsticks.Dependencies;
using Chopsticks.Dependencies.Contained;
using Chopsticks.Dependencies.Containers;
using IUnityDependencyExtensionsTests.Mocks;
using MonoContainerTests.Mocks;
using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine.TestTools;

namespace IUnityDependencyExtensionsTests
{
    public class RegisterAs
    {
        [Test]
        public void RegisterAs_InvalidContract_ReturnsNull()
        {
            // Set up
            var unityDependency = Substitute.For<IMockMonoDependency>();
            unityDependency.Container = Substitute.For<MockDependencyContainer>();

            // Act
            LogAssert.ignoreFailingMessages = true;
            var registration = unityDependency.RegisterAs<MockDependencyContainer, 
                MockMonoContainerService, string>(); // unityDependency does not implement 'string'.
            LogAssert.ignoreFailingMessages = false;

            // Assert
            Assert.That(registration, Is.Null);
        }

        [Test]
        public void RegisterAs_ValidContract_AddsToDependencyRegistrations()
        {
            // Set up
            List<DependencyRegistration> registrations = new();

            var unityDependency = Substitute.For<IMockMonoDependency>();
            unityDependency.Container = Substitute.For<MockDependencyContainer>();
            unityDependency.Registrations.Returns(registrations);

            // Act
            var registration = unityDependency.RegisterAs<MockDependencyContainer,
                MockMonoContainerService, ITestContract>();

            // Assert
            Assert.That(registrations.Contains(registration), Is.True);
        }

        [Test]
        public void RegisterAs_ValidContract_RegistersWithContainer()
        {
            // Set up
            List<DependencyRegistration> registrations = new();

            var unityDependency = Substitute.For<IMockRegistrationMonoDependency>();
            unityDependency.Container = new MockRegistrationDependencyContainer();
            unityDependency.Registrations.Returns(registrations);

            // Act
            var registration = unityDependency.RegisterAs<MockRegistrationDependencyContainer,
                MockRegistrationContainerService, ITestContract>();

            // Assert
            Assert.That(unityDependency.Container.RegistrationWasCalled, Is.True);
            Assert.That(unityDependency.Container.ProvidedRegistration, Is.EqualTo(registration));
            Assert.That(unityDependency.Container.RegistrationSpecification.Contract, 
                Is.EqualTo(typeof(ITestContract)));
            Assert.That(unityDependency.Container.RegistrationSpecification.Lifetime,
                Is.EqualTo(DependencyLifetime.Singleton));
            Assert.That(unityDependency.Container.RegistrationSpecification.ImplementationFactory(null),
                Is.EqualTo(unityDependency));
        }
    }
}