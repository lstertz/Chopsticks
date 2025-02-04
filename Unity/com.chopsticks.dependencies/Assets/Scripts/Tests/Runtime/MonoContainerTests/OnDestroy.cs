using MonoContainerTests.Mocks;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;

namespace MonoContainerTests
{
    public class OnDestroy
    {
        [Test]
        public void OnDestroy_ComponentDestruction_DisposesOfInternalContainer()
        {
            // Set up
            var gameObject = new GameObject();
            var container = gameObject.AddComponent<MockMonoContainer>();

            // Act
            Object.DestroyImmediate(container);

            // Assert
            container.InternalContainer.Received(1).Dispose();
        }
    }
}