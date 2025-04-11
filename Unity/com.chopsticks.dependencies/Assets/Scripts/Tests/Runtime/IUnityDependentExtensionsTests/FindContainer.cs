using Chopsticks.Dependencies.Contained;
using Chopsticks.Dependencies.Containers;
using Chopsticks.Dependencies.Services;
using IUnityDependentExtensionsTests.Mocks;
using MonoContainerTests.Mocks;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IUnityDependentExtensionsTests
{

    public class FindContainer
    {

        [SetUp]
        public void SetUpScene()
        {
            SceneManager.LoadScene("StandardTestScene");
        }


        [Test]
        public void FindContainer_StandardCall_ReturnsFromServiceGetContainer()
        {
            // Set up
            var expectedContainer = Substitute.For<MockDependencyContainer>();
            var mockService = (expectedContainer as IUnityContainerServiceProvider<MockDependencyContainer, 
                MockMonoContainerService>).Service.Sub;
            var unityDependent = Substitute.For<MockUnityDependent>();
            unityDependent.Container = null;

            var transform = new GameObject().transform;
            var overrideContainer = Substitute.For<IUnityContainer<MockDependencyContainer>>();
            
            var setting = ContainerRetrievalSetting.Override;
            unityDependent.ContainerSetting.Returns(setting);
            
            mockService.GetContainer((ContainerSetting)setting, true, transform, 
                overrideContainer).Returns(expectedContainer);

            // Act
            var container = unityDependent.FindContainer(transform, overrideContainer);

            // Assert
            Assert.That(container, Is.EqualTo(expectedContainer));
            mockService.Received(1).GetContainer((ContainerSetting)setting, true, 
                transform, overrideContainer);
        }
    }
}