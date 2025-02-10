using Chopsticks.Dependencies.Contained;
using Chopsticks.Dependencies.Containers;
using MonoContainerTests.Mocks;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;

namespace IUnityDependentExtensionsTests
{
    public class FindContainer
    {
        [Test]
        public void FindContainer_StandardCall_ReturnsFromServiceGetContainer()
        {
            // Set up
            var mockService = IUnityDependent<MockDependencyContainer,
                MockMonoContainerService>.UnityContainerService;
            var expectedContainer = Substitute.For<MockDependencyContainer>();
            var unityDependent = Substitute.For<IUnityDependent<MockDependencyContainer, 
                MockMonoContainerService>>();

            var transform = new GameObject().transform;
            var overrideContainer = Substitute.For<IUnityContainer<MockDependencyContainer>>();
            
            var setting = ContainerSetting.Override;
            unityDependent.ContainerSetting.Returns(setting);
            
            mockService.Sub.GetContainer((ContainerRetrievalSetting)setting, true, transform, 
                overrideContainer).Returns(expectedContainer);

            // Act
            var container = unityDependent.FindContainer(transform, overrideContainer);

            // Assert
            Assert.That(container, Is.EqualTo(expectedContainer));
            mockService.Sub.Received(1).GetContainer((ContainerRetrievalSetting)setting, true, 
                transform, overrideContainer);
        }
    }
}