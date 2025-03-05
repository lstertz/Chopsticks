using MonoContainerTests.Mocks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MonoContainerTests
{
    public class RegisterNativeDependencies
    {
        [SetUp]
        public void SetUpScene()
        {
            SceneManager.LoadScene("StandardTestScene");
        }


        [Test]
        public void RegisterNativeDependencies_OnAwake_WasCalled()
        {
            // Set up
            var gameObject = new GameObject();

            // Act
            var container = gameObject.AddComponent<MockMonoContainer>();

            // Assert
            Assert.That(container.HasRegisteredNativeDependencies, Is.True);
        }
    }
}