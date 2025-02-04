using Chopsticks.Dependencies.Containers;
using NUnit.Framework;
using UnityEngine;

using UnityContainerService = Chopsticks.Dependencies.Containers.UnityContainerService<
    Chopsticks.Dependencies.Containers.DependencyContainer,
    Chopsticks.Dependencies.Factories.DefaultDependencyContainerFactory,
    Chopsticks.Dependencies.Containers.DependencyContainerDefinition>;

namespace UnityDependencyContainerServiceTests
{
    public class FindParentContainer
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
        public void FindParentContainer_GlobalParentSetting_Global()
        {
            // Set up
            var service = SetUp.StandardContainer(out var container);

            // Act
            var parentContainer = service.FindParentContainer(
                ContainerRetrievalSetting.Global, container, (MonoContainer)null);

            // Assert
            Assert.That(parentContainer, Is.EqualTo(service.GlobalContainer));
        }

        [Test]
        public void FindParentContainer_HierarchyWithGlobalRetrievalSetting_WithNoParent_Global()
        {
            // Set up
            var service = SetUp.StandardContainer(out var container);

            // Act
            var parentContainer = service.FindParentContainer(
                ContainerRetrievalSetting.HierarchyWithGlobal, container, (MonoContainer)null);

            // Assert
            Assert.That(parentContainer, Is.EqualTo(MonoContainer.Global));
        }

        [Test]
        public void FindParentContainer_HierarchyWithGlobalRetrievalSetting_WithParent_Parent()
        {
            // Set up
            var service = SetUp.ParentedContainers(out var child, out var parent);

            // Act
            var parentContainer = service.FindParentContainer(
                ContainerRetrievalSetting.HierarchyWithGlobal, child, (MonoContainer)null);

            // Assert
            Assert.That(parentContainer, Is.EqualTo(
                (parent as IUnityContainer<DependencyContainer>).NativeContainer));
        }

        [Test]
        public void FindParentContainer_HierarchyWithoutGlobalRetrievalSetting_WithNoParent_Null()
        {
            // Set up
            var service = SetUp.StandardContainer(out var container);

            // Act
            var parentContainer = service.FindParentContainer(
                ContainerRetrievalSetting.HierarchyWithoutGlobal, container, (MonoContainer)null);

            // Assert
            Assert.That(parentContainer, Is.Null);
        }

        [Test]
        public void FindParentContainer_HierarchyWithoutGlobalRetrievalSetting_WithParent_Parent()
        {
            // Set up
            var service = SetUp.ParentedContainers(out var child, out var parent);

            // Act
            var parentContainer = service.FindParentContainer(
                ContainerRetrievalSetting.HierarchyWithoutGlobal, child, (MonoContainer)null);

            // Assert
            Assert.That(parentContainer, Is.EqualTo(
                (parent as IUnityContainer<DependencyContainer>).NativeContainer));
        }

        [Test]
        public void FindParentContainer_OverrideRetrievalSetting_ChildOverride_Null()
        {
            // Set up
            var service = SetUp.ParentedContainers(out var child, out var parent);

            // Act
            var parentContainer = service.FindParentContainer(
                ContainerRetrievalSetting.Override, parent, child);

            // Assert
            Assert.That(parentContainer, Is.Null);
        }

        [Test]
        public void FindParentContainer_OverrideRetrievalSetting_SelfOverride_Null()
        {
            // Set up
            var service = SetUp.StandardContainer(out var container);

            // Act
            var parentContainer = service.FindParentContainer(
                ContainerRetrievalSetting.Override, container, container);

            // Assert
            Assert.That(parentContainer, Is.Null);
        }

        [Test]
        public void FindParentContainer_OverrideRetrievalSetting_ValidOverride_OverrideNativeContainer()
        {
            // Set up
            var service = SetUp.StandardContainer(out var container);

            var gameObject = new GameObject("Override Object");
            var overrideContainer = gameObject.AddComponent<MonoContainer>();

            // Act
            var parentContainer = service.FindParentContainer(
                ContainerRetrievalSetting.Override, container, overrideContainer);

            // Assert
            Assert.That(parentContainer, Is.EqualTo(
                (overrideContainer as IUnityContainer<DependencyContainer>).NativeContainer));
        }
    }
}