using Chopsticks.Dependencies.Containers;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using ContainerService = Chopsticks.Dependencies.Services.UnityContainerService<
    Chopsticks.Dependencies.Containers.DependencyContainer,
    Chopsticks.Dependencies.Factories.DefaultDependencyContainerFactory,
    Chopsticks.Dependencies.Containers.DependencyContainerDefinition>;

namespace UnityContainerServiceTests
{
    public class GetContainerFromHierarchy
    {
        public class SetUp
        {
            public static ContainerService SelfAsContainerWithParent(
                out MonoContainer childContainer, out MonoContainer parentContainer)
            {
                var service = new ContainerService();

                var parentGameObject = new GameObject("Parent Object");
                var gameObject = new GameObject("Test Object");
                gameObject.transform.parent = parentGameObject.transform;
                gameObject.SetActive(false);

                childContainer = gameObject.AddComponent<MonoContainer>();
                parentContainer = parentGameObject.AddComponent<MonoContainer>();

                gameObject.SetActive(true);

                return service;
            }

            public static ContainerService SelfAsContainer(
                out MonoContainer container)
            {
                var service = new ContainerService();

                var gameObject = new GameObject("Test Object");

                container = gameObject.AddComponent<MonoContainer>();

                return service;
            }
            public static ContainerService SelfNotAsContainerWithParent(
                out GameObject gameObject, out MonoContainer parentContainer)
            {
                var service = new ContainerService();

                var parentGameObject = new GameObject("Parent Object");
                gameObject = new GameObject("Test Object");
                gameObject.transform.parent = parentGameObject.transform;
                gameObject.SetActive(false);

                parentContainer = parentGameObject.AddComponent<MonoContainer>();

                gameObject.SetActive(true);

                return service;
            }
        }

        [SetUp]
        public void SetUpScene()
        {
            SceneManager.LoadScene("StandardTestScene");
        }


        [Test]
        [TestCase(true, true)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(false, false)]
        public void GetContainerFromHierarchy_SelfAsContainerWithoutParent_ProvidesPerSettings(
            bool includeSelf, bool fallbackToGlobal)
        {
            // Set up
            var service = SetUp.SelfAsContainer(out var container);

            // Act
            var serviceContainer = service.GetContainerFromHierarchy(container.transform,
                includeSelf, fallbackToGlobal);

            // Assert
            if (!includeSelf && fallbackToGlobal)
                Assert.That(serviceContainer, Is.EqualTo(ContainerService.GlobalContainer));
            else if (!includeSelf && !fallbackToGlobal)
                Assert.That(serviceContainer, Is.Null);
            else if (includeSelf)
                Assert.That(serviceContainer, Is.EqualTo(
                    (container as IUnityContainer<DependencyContainer>).NativeContainer));
        }

        [Test]
        [TestCase(true, true)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(false, false)]
        public void GetContainerFromHierarchy_SelfAsContainerWithParent_ProvidesPerSettings(
            bool includeSelf, bool fallbackToGlobal)
        {
            // Set up
            var service = SetUp.SelfAsContainerWithParent(out var child, out var parent);

            // Act
            var serviceContainer = service.GetContainerFromHierarchy(child.transform,
                includeSelf, fallbackToGlobal);

            // Assert
            if (!includeSelf)
                Assert.That(serviceContainer, Is.EqualTo(
                    (parent as IUnityContainer<DependencyContainer>).NativeContainer));
            else
                Assert.That(serviceContainer, Is.EqualTo(
                    (child as IUnityContainer<DependencyContainer>).NativeContainer));
        }

        [Test]
        [TestCase(true, true)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(false, false)]
        public void GetContainerFromHierarchy_SelfNotAsContainerWithoutParent_ProvidesPerSettings(
            bool includeSelf, bool fallbackToGlobal)
        {
            // Set up
            var service = new ContainerService();

            var gameObject = new GameObject("Test Object");
            gameObject.SetActive(false);

            // Act
            var serviceContainer = service.GetContainerFromHierarchy(gameObject.transform,
                includeSelf, fallbackToGlobal);

            // Assert
            if (fallbackToGlobal)
                Assert.That(serviceContainer, Is.EqualTo(ContainerService.GlobalContainer));
            else
                Assert.That(serviceContainer, Is.Null);
        }

        [Test]
        [TestCase(true, true)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(false, false)]
        public void GetContainerFromHierarchy_SelfNotAsContainerWithParent_ProvidesPerSettings(
            bool includeSelf, bool fallbackToGlobal)
        {
            // Set up
            var service = SetUp.SelfNotAsContainerWithParent(out var child, out var parent);

            // Act
            var serviceContainer = service.GetContainerFromHierarchy(child.transform,
                includeSelf, fallbackToGlobal);

            // Assert
            Assert.That(serviceContainer, Is.EqualTo(
                (parent as IUnityContainer<DependencyContainer>).NativeContainer));
        }
    }
}