using Chopsticks.Dependencies.Services;
using MonoContainerTests.Mocks;
using NUnit.Framework;
using UnityEngine.SceneManagement;

namespace UnityContainerServiceTests
{
    public class BuildContainer
    {
        [SetUp]
        public void SetUpScene()
        {
            SceneManager.LoadScene("StandardTestScene");
        }


        [Test]
        public void BuildContainer_WithDefinition_UsesFactoryWithDefintion()
        {
            // Arrange
            var service = new UnityContainerService<MockDependencyContainer, 
                MockDependencyContainerFactory, MockDependencyContainer.Definition>();
            var definition = new MockDependencyContainer.Definition();

            // Act
            var container = service.BuildContainer(definition);

            // Assert
            var call = MockDependencyContainerFactory.Received.BuildContainerCall;
            Assert.That(call.WasCalled, Is.True);
            Assert.That(call.Parameter, Is.EqualTo(definition));
            Assert.That(container, Is.Not.Null);

            // Clean up
            MockDependencyContainerFactory.ResetMockState();
        }

        [Test]
        public void BuildContainer_WithoutDefinition_UsesFactoryWithDefault()
        {
            // Arrange
            var service = new UnityContainerService<MockDependencyContainer, 
                MockDependencyContainerFactory, MockDependencyContainer.Definition>();
            MockDependencyContainer.Definition definition = default;

            // Act
            var container = service.BuildContainer();

            // Assert
            var call = MockDependencyContainerFactory.Received.BuildContainerCall;
            Assert.That(call.WasCalled, Is.True);
            Assert.That(call.Parameter, Is.EqualTo(definition));
            Assert.That(container, Is.Not.Null);

            // Clean up
            MockDependencyContainerFactory.ResetMockState();
        }
    }
}