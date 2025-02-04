using MonoContainerTests.Mocks;
using NSubstitute;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MonoContainerTests
{
    public class ResolveAll
    {
        public static class Mock
        {
            public interface IContract { }
        }

        [Test]
        public void ResolveAll_StandardCall_ProvidesFromInternalContainer()
        {
            // Set up
            var implementationA = Substitute.For<Mock.IContract>();
            var implementationB = Substitute.For<Mock.IContract>();
            IEnumerable<object> expectedImplementations = new object[2]
            {
                implementationA,
                implementationB
            };

            var gameObject = new GameObject();
            var container = gameObject.AddComponent<MockMonoContainer>();
            container.InternalContainer.ResolveAll(typeof(Mock.IContract))
                .Returns(expectedImplementations);

            // Act
            var implementations = container.ResolveAll(typeof(Mock.IContract)).ToArray();

            // Assert
            container.InternalContainer.Received(1).ResolveAll(typeof(Mock.IContract));
            Assert.That(implementations, Is.EquivalentTo(expectedImplementations));
        }
    }
}