using Chopsticks.Dependencies.Containers;
using Chopsticks.Dependencies.Factories;
using NUnit.Framework;

namespace UnityDependencyContainerServiceTests
{
    public class GlobalContainer
    {
        [Test]
        public void GlobalContainer_AfterReset_NewCallInstance()
        {
            // Set up
            var service = new UnityContainerService<DependencyContainer,
                DefaultDependencyContainerFactory, DependencyContainerDefinition>();
            var firstCallContainer = service.GlobalContainer;
            service.ResetGlobal();

            // Act
            var afterResetContainer = service.GlobalContainer;

            // Assert
            Assert.That(afterResetContainer, Is.Not.Null);
            Assert.That(afterResetContainer, Is.Not.EqualTo(firstCallContainer));
        }

        [Test]
        public void GlobalContainer_FirstCall_IsNotNull()
        {
            // Set up
            var service = new UnityContainerService<DependencyContainer,
                DefaultDependencyContainerFactory, DependencyContainerDefinition>();

            // Act
            var container = service.GlobalContainer;

            // Assert
            Assert.That(container, Is.Not.Null);
        }

        [Test]
        public void GlobalContainer_SecondCall_MatchesFirstCallInstance()
        {
            // Set up
            var service = new UnityContainerService<DependencyContainer,
                DefaultDependencyContainerFactory, DependencyContainerDefinition>();
            var firstCallContainer = service.GlobalContainer;

            // Act
            var secondCallContainer = service.GlobalContainer;

            // Assert
            Assert.That(secondCallContainer, Is.EqualTo(firstCallContainer));
        }
    }
}