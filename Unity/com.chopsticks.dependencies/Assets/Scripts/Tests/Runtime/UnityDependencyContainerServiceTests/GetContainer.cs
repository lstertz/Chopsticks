using Chopsticks.Dependencies.Containers;
using NUnit.Framework;
using UnityEngine;

using UnityContainerService = Chopsticks.Dependencies.Containers.UnityContainerService<
    Chopsticks.Dependencies.Containers.DependencyContainer,
    Chopsticks.Dependencies.Factories.DefaultDependencyContainerFactory,
    Chopsticks.Dependencies.Containers.DependencyContainerDefinition>;

namespace UnityDependencyContainerServiceTests
{
    public class GetContainer
    {
        public class SetUp
        {
            public static UnityContainerService ParentedContainers(
                out MonoContainer childContainer, out MonoContainer parentContainer)
            {
                var service = new UnityContainerService();

                var parentGameObject = new GameObject("Parent Object");
                var gameObject = new GameObject("Test Object");
                gameObject.transform.parent = parentGameObject.transform;
                gameObject.SetActive(false);

                childContainer = gameObject.AddComponent<MonoContainer>();
                parentContainer = parentGameObject.AddComponent<MonoContainer>();

                gameObject.SetActive(true);

                return service;
            }

            public static UnityContainerService StandardContainer(
                out MonoContainer container)
            {
                var service = new UnityContainerService();

                var gameObject = new GameObject("Test Object");
                gameObject.SetActive(false);

                container = gameObject.AddComponent<MonoContainer>();

                return service;
            }
        }


        [Test]
        public void GetContainer_GlobalRetrievalSetting_Global()
        {
            // Set up
            var service = new UnityContainerService();

            // Act
            var serviceContainer = service.GetContainer(
                ContainerRetrievalSetting.Global,
                false, (MonoContainer)null, (MonoContainer)null);

            // Assert
            Assert.That(serviceContainer, Is.EqualTo(service.GlobalContainer));
        }

        [Test]
        public void GetContainer_HierarchyWithGlobalRetrievalSetting_IncludeSelf_Self()
        {
            // Set up
            var service = SetUp.ParentedContainers(out var child, out var parent);

            // Act
            var serviceContainer = service.GetContainer(
                ContainerRetrievalSetting.HierarchyWithGlobal, 
                true, child, (MonoContainer)null);

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
                false, container, (MonoContainer)null);

            // Assert
            Assert.That(serviceContainer, Is.EqualTo(MonoContainer.Global));
        }

        [Test]
        public void GetContainer_HierarchyWithGlobalRetrievalSetting_WithParent_Parent()
        {
            // Set up
            var service = SetUp.ParentedContainers(out var child, out var parent);

            // Act
            var serviceContainer = service.GetContainer(
                ContainerRetrievalSetting.HierarchyWithGlobal, 
                false, child, (MonoContainer)null);

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
                true, child, (MonoContainer)null);

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
                false, container, (MonoContainer)null);

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
                false, child, (MonoContainer)null);

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
                false, (MonoContainer)null, overrideContainer);

            // Assert
            Assert.That(serviceContainer, Is.EqualTo(
                (overrideContainer as IUnityContainer<DependencyContainer>).NativeContainer));
        }
    }
}