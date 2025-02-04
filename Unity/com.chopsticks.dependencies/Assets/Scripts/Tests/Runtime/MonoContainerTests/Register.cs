using Chopsticks.Dependencies.Containers;
using MonoContainerTests.Mocks;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;

namespace MonoContainerTests
{
    public class Register
    {
        public static class Mock
        {
            public interface IContract { }
        }


        [Test]
        public void Register_StandardCall_CallsInternalContainer()
        {
            // Set up
            var expectedRegistration = new DependencyRegistration()
            {
                Contract = typeof(Mock.IContract)
            };
            var specification = new DependencySpecification()
            {
                Contract = typeof(Mock.IContract),
                ImplementationFactory = _ => Substitute.For<Mock.IContract>()
            };

            var gameObject = new GameObject();
            var container = gameObject.AddComponent<MockMonoContainer>();
            container.InternalContainer.Register(specification, out _)
                .ReturnsForAnyArgs(x =>
                {
                    x[1] = expectedRegistration;
                    return container.InternalContainer;
                });

            // Act
            container.Register(specification, out var registration);

            // Assert
            container.InternalContainer.Received(1).Register(specification, 
                out Arg.Any<DependencyRegistration>());
            Assert.That(registration, Is.EqualTo(expectedRegistration));
        }

        [Test]
        public void Register_StandardCall_ReturnsMonoContainer()
        {
            // Set up
            var specification = new DependencySpecification()
            {
                Contract = typeof(Mock.IContract),
                ImplementationFactory = _ => Substitute.For<Mock.IContract>()
            };

            var gameObject = new GameObject();
            var container = gameObject.AddComponent<MockMonoContainer>();

            // Act
            var returnContainer = container.Register(specification, out _);

            // Assert
            Assert.That(returnContainer, Is.EqualTo(container));
        }
    }
}