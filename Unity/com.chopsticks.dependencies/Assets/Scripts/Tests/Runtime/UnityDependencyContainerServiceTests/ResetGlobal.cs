using MonoContainerTests.Mocks;
using NSubstitute;
using NUnit.Framework;

using ContainerService = Chopsticks.Dependencies.Services.UnityContainerService<
    MonoContainerTests.Mocks.MockDependencyContainer,
    MonoContainerTests.Mocks.MockDependencyContainerFactory, 
    MonoContainerTests.Mocks.MockDependencyContainer.Definition>;

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
            var container = ContainerService.GlobalContainer;

            // Act
            ContainerService.ResetGlobal();

            // Assert
            container.Received(1).Dispose();
        }

        [Test]
        public void ResetGlobal_WithDefinition_CreatesNewWithDefinition()
        {
            // Set up
            var container = ContainerService.GlobalContainer;
            var definition = new MockDependencyContainer.Definition();

            // Act
            ContainerService.ResetGlobal(definition);

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
            var container = ContainerService.GlobalContainer;

            // Act
            ContainerService.ResetGlobal();

            // Assert
            Assert.That(MockDependencyContainerFactory.Received
                .BuildContainerCall.WasCalled, Is.True);
            Assert.That(MockDependencyContainerFactory.Received
                .BuildContainerCall.Parameter, Is.EqualTo(null));
        }
    }
}