using MonoContainerTests.Mocks;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MonoContainerTests
{
    public class OnDestroy
    {
        [SetUp]
        public void SetUpScene()
        {
            SceneManager.LoadScene("StandardTestScene");
        }


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