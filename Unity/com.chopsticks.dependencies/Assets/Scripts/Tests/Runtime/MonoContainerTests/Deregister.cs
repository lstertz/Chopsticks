using Chopsticks.Dependencies.Containers;
using MonoContainerTests.Mocks;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;

namespace MonoContainerTests
{
    public class Deregister
    {
        public static class Mock
        {
            public interface IContract { }
        }


        [Test]
        public void Deregister_StandardCall_CallsInternalContainer()
        {
            // Set up
            var registration = new DependencyRegistration()
            {
                Contract = typeof(Mock.IContract)
            };

            var gameObject = new GameObject();
            var container = gameObject.AddComponent<MockMonoContainer>();

            // Act
            container.Deregister(registration);

            // Assert
            container.InternalContainer.Received(1).Deregister(registration);
        }

        [Test]
        public void Deregister_StandardCall_ReturnsMonoContainer()
        {
            // Set up
            var gameObject = new GameObject();
            var container = gameObject.AddComponent<MockMonoContainer>();

            var registration = new DependencyRegistration()
            {
                Contract = typeof(Mock.IContract)
            };

            // Act
            var returnContainer = container.Deregister(registration);

            // Assert
            Assert.That(returnContainer, Is.EqualTo(container));
        }
    }
}