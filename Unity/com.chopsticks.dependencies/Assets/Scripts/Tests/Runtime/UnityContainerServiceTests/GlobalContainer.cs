using NUnit.Framework;

using ContainerService = Chopsticks.Dependencies.Services.UnityContainerService<
    MonoContainerTests.Mocks.MockDependencyContainer,
    MonoContainerTests.Mocks.MockDependencyContainerFactory,
    MonoContainerTests.Mocks.MockDependencyContainer.Definition>;

namespace UnityContainerServiceTests
{
    public class GlobalContainer
    {
        [Test]
        public void GlobalContainer_AfterReset_NewCallInstance()
        {
            // Set up
            var firstCallContainer = ContainerService.GlobalContainer;
            ContainerService.ResetGlobal();

            // Act
            var afterResetContainer = ContainerService.GlobalContainer;

            // Assert
            Assert.That(afterResetContainer, Is.Not.Null);
            Assert.That(afterResetContainer, Is.Not.EqualTo(firstCallContainer));
        }

        [Test]
        public void GlobalContainer_FirstCall_IsNotNull()
        {
            // Set up & Act
            var container = ContainerService.GlobalContainer;

            // Assert
            Assert.That(container, Is.Not.Null);
        }

        [Test]
        public void GlobalContainer_SecondCall_MatchesFirstCallInstance()
        {
            // Set up
            var firstCallContainer = ContainerService.GlobalContainer;

            // Act
            var secondCallContainer = ContainerService.GlobalContainer;

            // Assert
            Assert.That(secondCallContainer, Is.EqualTo(firstCallContainer));
        }
    }
}