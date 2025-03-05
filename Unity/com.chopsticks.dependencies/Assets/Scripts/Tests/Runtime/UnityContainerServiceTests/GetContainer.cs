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
    public class GetContainer
    {
        public class SetUp
        {
            public static ContainerService ParentedContainers(
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

            public static ContainerService StandardContainer(
                out MonoContainer container)
            {
                var service = new ContainerService();

                var gameObject = new GameObject("Test Object");
                gameObject.SetActive(false);

                container = gameObject.AddComponent<MonoContainer>();

                return service;
            }
        }

        [SetUp]
        public void SetUpScene()
        {
            SceneManager.LoadScene("StandardTestScene");
        }


        [Test]
        public void GetContainer_GlobalRetrievalSetting_Global()
        {
            // Set up
            var service = new ContainerService();

            // Act
            var serviceContainer = service.GetContainer(
                ContainerRetrievalSetting.Global,
                false, (Transform)null, (MonoContainer)null);

            // Assert
            Assert.That(serviceContainer, Is.EqualTo(ContainerService.GlobalContainer));
        }

        [Test]
        public void GetContainer_HierarchyWithGlobalRetrievalSetting_IncludeSelf_Self()
        {
            // Set up
            var service = SetUp.ParentedContainers(out var child, out var parent);

            // Act
            var serviceContainer = service.GetContainer(
                ContainerRetrievalSetting.HierarchyWithGlobal, 
                true, child.transform, (MonoContainer)null);

            // Assert
            Assert.That(serviceContainer, Is.EqualTo(
                (child as IUnityContainer<DependencyContainer>).NativeContainer));
        }

        [Test]
        public void GetContainer_HierarchyWithGlobalRetrievalSetting_WithNoParent_Global()
        {
            // Set up
            var service = SetUp.StandardContainer(out var container);

            // Act
            var serviceContainer = service.GetContainer(
                ContainerRetrievalSetting.HierarchyWithGlobal, 
                false, container.transform, (MonoContainer)null);

            // Assert
            Assert.That(serviceContainer, Is.EqualTo(ContainerService.GlobalContainer));
        }

        [Test]
        public void GetContainer_HierarchyWithGlobalRetrievalSetting_WithParent_Parent()
        {
            // Set up
            var service = SetUp.ParentedContainers(out var child, out var parent);

            // Act
            var serviceContainer = service.GetContainer(
                ContainerRetrievalSetting.HierarchyWithGlobal, 
                false, child.transform, (MonoContainer)null);

            // Assert
            Assert.That(serviceContainer, Is.EqualTo(
                (parent as IUnityContainer<DependencyContainer>).NativeContainer));
        }

        [Test]
        public void GetContainer_HierarchyWithoutGlobalRetrievalSetting_IncludeSelf_Self()
        {
            // Set up
            var service = SetUp.ParentedContainers(out var child, out var parent);

            // Act
            var serviceContainer = service.GetContainer(
                ContainerRetrievalSetting.HierarchyWithoutGlobal, 
                true, child.transform, (MonoContainer)null);

            // Assert
            Assert.That(serviceContainer, Is.EqualTo(
                (child as IUnityContainer<DependencyContainer>).NativeContainer));
        }

        [Test]
        public void GetContainer_HierarchyWithoutGlobalRetrievalSetting_WithNoParent_Null()
        {
            // Set up
            var service = SetUp.StandardContainer(out var container);

            // Act
            var serviceContainer = service.GetContainer(
                ContainerRetrievalSetting.HierarchyWithoutGlobal, 
                false, container.transform, (MonoContainer)null);

            // Assert
            Assert.That(serviceContainer, Is.Null);
        }

        [Test]
        public void GetContainer_HierarchyWithoutGlobalRetrievalSetting_WithParent_Parent()
        {
            // Set up
            var service = SetUp.ParentedContainers(out var child, out var parent);

            // Act
            var serviceContainer = service.GetContainer(
                ContainerRetrievalSetting.HierarchyWithoutGlobal, 
                false, child.transform, (MonoContainer)null);

            // Assert
            Assert.That(serviceContainer, Is.EqualTo(
                (parent as IUnityContainer<DependencyContainer>).NativeContainer));
        }

        [Test]
        public void GetContainer_OverrideRetrievalSetting_OverrideNativeContainer()
        {
            // Set up
            var service = SetUp.StandardContainer(out var overrideContainer);

            // Act
            var serviceContainer = service.GetContainer(
                ContainerRetrievalSetting.Override,
                false, (Transform)null, overrideContainer);

            // Assert
            Assert.That(serviceContainer, Is.EqualTo(
                (overrideContainer as IUnityContainer<DependencyContainer>).NativeContainer));
        }
    }
}