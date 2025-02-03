using Chopsticks.Dependencies.Containers;
using MonoContainerTests.Mocks;
using NSubstitute;
using NUnit.Framework;

namespace UnityDependencyContainerServiceTests
{
    public class ResetGlobal
    {
        [TearDown]
        public void TearDown()
        {
            MockDependencyContainerFactory.ResetMockState();
        }


        [Test]
        public void ResetGlobal_StandardReset_DisposesOfInstance()
        {
            // Set up
            var service = new UnityContainerService<MockDependencyContainer, 
                MockDependencyContainerFactory, MockDependencyContainer.Definition>();
            var container = service.GlobalContainer;

            // Act
            service.ResetGlobal();

            // Assert
            container.Received(1).Dispose();
        }

        [Test]
        public void ResetGlobal_WithDefinition_CreatesNewWithDefinition()
        {
            // Set up
            var service = new UnityContainerService<MockDependencyContainer,
                MockDependencyContainerFactory, MockDependencyContainer.Definition>();
            var container = service.GlobalContainer;

            var definition = new MockDependencyContainer.Definition();

            // Act
            service.ResetGlobal(definition);

            // Assert
            Assert.That(MockDependencyContainerFactory.Received
                .BuildContainerCall.WasCalled, Is.True);
            Assert.That(MockDependencyContainerFactory.Received
                .BuildContainerCall.Parameter, Is.EqualTo(definition));
        }

        [Test]
        public void ResetGlobal_WithoutDefinition_CreatesNewDefault()
        {
            // Set up
            var service = new UnityContainerService<MockDependencyContainer,
                MockDependencyContainerFactory, MockDependencyContainer.Definition>();
            var container = service.GlobalContainer;

            // Act
            service.ResetGlobal();

            // Assert
            Assert.That(MockDependencyContainerFactory.Received
                .BuildContainerCall.WasCalled, Is.True);
            Assert.That(MockDependencyContainerFactory.Received
                .BuildContainerCall.Parameter, Is.EqualTo(null));
        }
    }
}