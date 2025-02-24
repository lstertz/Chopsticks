using Chopsticks.Dependencies;
using Chopsticks.Dependencies.Contained;
using IUnityDependencyExtensionsTests.Mocks;
using MonoContainerTests.Mocks;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace IUnityDependencyExtensionsTests
{
    public class RegisterAs
    {
        [Test]
        public void RegisterAs_InvalidContract_ReturnsNull()
        {
            // Set up
            var unityDependency = new GameObject().AddComponent<MockMonoDependency>()
                as IUnityDependency<MockDependencyContainer, MockMonoContainerService>;
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
            var unityDependency = new GameObject().AddComponent<MockMonoDependency>()
                as IUnityDependency<MockDependencyContainer, MockMonoContainerService>;
            unityDependency.Container = Substitute.For<MockDependencyContainer>();

            // Act
            var registration = unityDependency.RegisterAs<MockDependencyContainer,
                MockMonoContainerService, IMockMonoDependency>();

            // Assert
            Assert.That(unityDependency.Registrations.Contains(registration), Is.True);
        }

        [Test]
        public void RegisterAs_ValidContract_RegistersWithContainer()
        {
            // Set up
            var unityDependency = new GameObject().AddComponent<MockRegistrationMonoDependency>()
                as IUnityDependency<MockRegistrationDependencyContainer, MockRegistrationContainerService>;
            unityDependency.Container = new MockRegistrationDependencyContainer();

            // Act
            var registration = unityDependency.RegisterAs<MockRegistrationDependencyContainer,
                MockRegistrationContainerService, IMockMonoDependency>();

            // Assert
            Assert.That(unityDependency.Container.RegistrationWasCalled, Is.True);
            Assert.That(unityDependency.Container.ProvidedRegistration, Is.EqualTo(registration));
            Assert.That(unityDependency.Container.RegistrationSpecification.Contract, 
                Is.EqualTo(typeof(IMockMonoDependency)));
            Assert.That(unityDependency.Container.RegistrationSpecification.Lifetime,
                Is.EqualTo(DependencyLifetime.Singleton));
            Assert.That(unityDependency.Container.RegistrationSpecification.ImplementationFactory(null),
                Is.EqualTo(unityDependency));
        }
    }
}