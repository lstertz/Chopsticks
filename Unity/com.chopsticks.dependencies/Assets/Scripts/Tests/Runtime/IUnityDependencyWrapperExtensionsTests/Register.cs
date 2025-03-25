using Chopsticks.Dependencies;
using Chopsticks.Dependencies.Contained;
using Chopsticks.Dependencies.Containers;
using IUnityDependencyExtensionsTests.Mocks;
using IUnityDependencyWrapperExtensionsTests.Mocks;
using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;

namespace IUnityDependencyWrapperExtensionsTests
{
    public class Register
    {
        [Test]
        public void Register_Dependency_RegistersWithContainer()
        {
            // Setup
            List<DependencyRegistration> registrations = new();

            var mockContainer = new Mocks.MockRegistrationDependencyContainer();
            var dependencyWrapper = Substitute.For<IMockMonoDependencyWrapper>();
            dependencyWrapper.Container.Returns(mockContainer);
            dependencyWrapper.Registrations.Returns(registrations);

            var dependency = Substitute.For<ITestContract>();

            // Act
            var registration = dependencyWrapper.Register(dependency);

            // Assert
            Assert.That(mockContainer.RegistrationWasCalled, Is.True);
            Assert.That(mockContainer.ProvidedRegistration, Is.EqualTo(registration));
            Assert.That(mockContainer.RegistrationSpecification.Contract,
                Is.EqualTo(typeof(ITestContract)));
            Assert.That(mockContainer.RegistrationSpecification.Lifetime,
                Is.EqualTo(DependencyLifetime.Singleton));
            Assert.That(mockContainer.RegistrationSpecification.ImplementationFactory(null),
                Is.EqualTo(dependency));
        }

        [Test]
        public void Register_Dependency_WrapperTracksRegistration()
        {
            // Setup
            List<DependencyRegistration> registrations = new();

            var mockContainer = new Mocks.MockRegistrationDependencyContainer();
            var dependencyWrapper = Substitute.For<IMockMonoDependencyWrapper>();
            dependencyWrapper.Container.Returns(mockContainer);
            dependencyWrapper.Registrations.Returns(registrations);

            var dependency = Substitute.For<ITestContract>();

            // Act
            var registration = dependencyWrapper.Register(dependency);

            // Assert
            Assert.That(registrations.Count, Is.EqualTo(1));
            Assert.That(registrations.Contains(registration), Is.True);
        }


        [Test]
        public void Register_ImplementationFactory_RegistersWithContainer()
        {
            // Setup
            List<DependencyRegistration> registrations = new();

            var mockContainer = new Mocks.MockRegistrationDependencyContainer();
            var dependencyWrapper = Substitute.For<IMockMonoDependencyWrapper>();
            dependencyWrapper.Container.Returns(mockContainer);
            dependencyWrapper.Registrations.Returns(registrations);

            var dependency = Substitute.For<ITestContract>();

            // Act
            var registration = dependencyWrapper.Register(_ => dependency);

            // Assert
            Assert.That(mockContainer.RegistrationWasCalled, Is.True);
            Assert.That(mockContainer.ProvidedRegistration, Is.EqualTo(registration));
            Assert.That(mockContainer.RegistrationSpecification.Contract,
                Is.EqualTo(typeof(ITestContract)));
            Assert.That(mockContainer.RegistrationSpecification.Lifetime,
                Is.EqualTo(DependencyLifetime.Singleton));
            Assert.That(mockContainer.RegistrationSpecification.ImplementationFactory(null),
                Is.EqualTo(dependency));
        }

        [Test]
        public void Register_ImplementationFactory_WithLifetime_RegistersWithContainer()
        {
            // Setup
            List<DependencyRegistration> registrations = new();

            var mockContainer = new Mocks.MockRegistrationDependencyContainer();
            var dependencyWrapper = Substitute.For<IMockMonoDependencyWrapper>();
            dependencyWrapper.Container.Returns(mockContainer);
            dependencyWrapper.Registrations.Returns(registrations);

            var dependency = Substitute.For<ITestContract>();
            var lifetime = DependencyLifetime.Transient;

            // Act
            var registration = dependencyWrapper.Register(_ => dependency, lifetime);

            // Assert
            Assert.That(mockContainer.RegistrationWasCalled, Is.True);
            Assert.That(mockContainer.ProvidedRegistration, Is.EqualTo(registration));
            Assert.That(mockContainer.RegistrationSpecification.Contract,
                Is.EqualTo(typeof(ITestContract)));
            Assert.That(mockContainer.RegistrationSpecification.Lifetime,
                Is.EqualTo(lifetime));
            Assert.That(mockContainer.RegistrationSpecification.ImplementationFactory(null),
                Is.EqualTo(dependency));
        }

        [Test]
        public void Register_ImplementationFactory_WrapperTracksRegistration()
        {
            // Setup
            List<DependencyRegistration> registrations = new();

            var mockContainer = new Mocks.MockRegistrationDependencyContainer();
            var dependencyWrapper = Substitute.For<IMockMonoDependencyWrapper>();
            dependencyWrapper.Container.Returns(mockContainer);
            dependencyWrapper.Registrations.Returns(registrations);

            var dependency = Substitute.For<ITestContract>();

            // Act
            var registration = dependencyWrapper.Register(_ => dependency);

            // Assert
            Assert.That(registrations.Count, Is.EqualTo(1));
            Assert.That(registrations.Contains(registration), Is.True);
        }
    }
}