using MonoContainerTests.Mocks;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;

namespace MonoContainerTests
{
    public class Resolve
    {
        public static class Mock
        {
            public interface IContract { }
        }


        [Test]
        public void Resolve_KnownDependency_ProvidesFromInternalContainer()
        {
            // Set up
            var expectedImplementation = Substitute.For<Mock.IContract>();
            var isResolved = true;

            var gameObject = new GameObject();
            var container = gameObject.AddComponent<MockMonoContainer>();
            container.InternalContainer.Resolve(typeof(Mock.IContract), out _).Returns(x =>
            {
                x[1] = expectedImplementation;
                return isResolved;
            });

            // Act
            bool wasResolved = container.Resolve(typeof(Mock.IContract), out var implementation);

            // Assert
            container.InternalContainer.Received(1).Resolve(typeof(Mock.IContract), out _);
            Assert.That(implementation, Is.EqualTo(expectedImplementation));
            Assert.That(wasResolved, Is.EqualTo(isResolved));
        }

        [Test]
        public void Resolve_UnknownDependency_False()
        {
            // Set up
            Mock.IContract expectedImplementation = null;
            var isResolved = false;

            var gameObject = new GameObject();
            var container = gameObject.AddComponent<MockMonoContainer>();
            container.InternalContainer.Resolve(typeof(Mock.IContract), out _).Returns(x =>
            {
                x[1] = expectedImplementation;
                return isResolved;
            });

            // Act
            bool wasResolved = container.Resolve(typeof(Mock.IContract), out var implementation);

            // Assert
            container.InternalContainer.Received(1).Resolve(typeof(Mock.IContract), out _);
            Assert.That(implementation, Is.EqualTo(expectedImplementation));
            Assert.That(wasResolved, Is.EqualTo(isResolved));
        }
    }
}