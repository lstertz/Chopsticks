using Chopsticks.Dependencies.Containers;
using Chopsticks.Dependencies.Services;
using MonoContainerTests.Mocks;
using NSubstitute;
using NUnit.Framework;
using TestHelpers;
using UnityEngine;
using UnityEngine.TestTools;
using ParentSetting = Chopsticks.Dependencies.Containers.ContainerSetting;

namespace MonoContainerTests
{
    public class ContainerParentSetting
    {

        public class SetUp
        {
            public static MockMonoContainer StandardContainer(
                ParentSetting parentSetting,
                out GameObject containerGameObject,
                out MockMonoContainer parentContainer,
                out IUnityContainerService<MockDependencyContainer, 
                    MockDependencyContainer.Definition> serviceSub)
            {
                var parentGameObject = new GameObject("Parent Object");
                containerGameObject = new GameObject("Test Object");
                containerGameObject.SetActive(false);

                var container = containerGameObject.AddComponent<MockMonoContainer>();
                parentContainer = parentGameObject.AddComponent<MockMonoContainer>();

                container.transform.parent = parentContainer.transform;

                container.SetSerializedProperty("_containerParentSetting", parentSetting);
                container.SetSerializedProperty("_overrideParent", parentContainer);

                serviceSub = container.ContainerService.Sub;
                serviceSub.ClearReceivedCalls();

                return container;
            }
        }


        [Test]
        [TestCase(ParentSetting.Global)]
        [TestCase(ParentSetting.HierarchyWithGlobal)]
        [TestCase(ParentSetting.HierarchyWithoutGlobal)]
        [TestCase(ParentSetting.Override)]
        public void Awake_VariousParentSettings_SetsToRetrievedContainer(
            ParentSetting parentSetting)
        {
            // Set up
            var container = SetUp.StandardContainer(parentSetting,
                out var containerGameObject, out var parentContainer, out var serviceSub);

            serviceSub.FindParentContainer(
                (ContainerRetrievalSetting)parentSetting, container, parentContainer)
                .Returns(parentContainer.InternalContainer);

            // Act
            containerGameObject.SetActive(true);

            // Assert
            Assert.That(container.InternalContainer.Parent,
                Is.EqualTo(parentContainer.InternalContainer));
            serviceSub.Received(1).FindParentContainer(
                (ContainerRetrievalSetting)parentSetting, container, parentContainer);
        }

        [Test]
        public void Awake_NoneParentSetting_SetsToNull()
        {
            // Set up
            var container = SetUp.StandardContainer(ParentSetting.None,
                out var containerGameObject, out var parentContainer, out var serviceSub);

            // Act
            containerGameObject.SetActive(true);

            // Assert
            Assert.That(container.InternalContainer.Parent, Is.Null);
            serviceSub.DidNotReceiveWithAnyArgs().FindParentContainer(
                ContainerRetrievalSetting.Global, container, parentContainer);
        }

        [Test]
        public void Awake_OverrideParentSettingWithInvalidOverride_SetsOverrideParentToNull()
        {
            // Set up
            var container = SetUp.StandardContainer(ParentSetting.Override,
                out var containerGameObject, out var parentContainer, out var serviceSub);

            serviceSub.FindParentContainer(
                ContainerRetrievalSetting.Override, container, parentContainer)
                .Returns((MockDependencyContainer)null);

            // Act
            LogAssert.ignoreFailingMessages = true;
            containerGameObject.SetActive(true);
            LogAssert.ignoreFailingMessages = false;

            // Assert
            container.GetSerializedProperty("_overrideParent", out var overrideParent);

            Assert.That(overrideParent, Is.Null);
            serviceSub.Received(1).FindParentContainer(
                ContainerRetrievalSetting.Override, container, parentContainer);
        }


        [Test]
        [TestCase(ParentSetting.Global)]
        [TestCase(ParentSetting.HierarchyWithGlobal)]
        [TestCase(ParentSetting.HierarchyWithoutGlobal)]
        [TestCase(ParentSetting.Override)]
        public void OnTransformParentChanged_VariousParentSettings_SetsToRetrievedContainer(
            ParentSetting parentSetting)
        {
            // Set up
            var container = SetUp.StandardContainer(parentSetting,
                out _, out var parentContainer, out var serviceSub);

            serviceSub.FindParentContainer(
                (ContainerRetrievalSetting)parentSetting, container, parentContainer)
                .Returns(parentContainer.InternalContainer);

            // Act
            container.OnTransformParentChanged();

            // Assert
            Assert.That(container.InternalContainer.Parent,
                Is.EqualTo(parentContainer.InternalContainer));
            serviceSub.Received(1).FindParentContainer(
                (ContainerRetrievalSetting)parentSetting, container, parentContainer);
        }

        [Test]
        public void OnTransformParentChanged_NoneParentSetting_SetsToNull()
        {
            // Set up
            var container = SetUp.StandardContainer(ParentSetting.None,
                out _, out var parentContainer, out var serviceSub);

            // Act
            container.OnTransformParentChanged();

            // Assert
            Assert.That(container.InternalContainer.Parent, Is.Null);
            serviceSub.DidNotReceiveWithAnyArgs().FindParentContainer(
                ContainerRetrievalSetting.Global, container, parentContainer);
        }

        [Test]
        public void OnTransformParentChanged_OverrideParentSettingWithInvalidOverride_SetsOverrideParentToNull()
        {
            // Set up
            var container = SetUp.StandardContainer(ParentSetting.Override,
                out _, out var parentContainer, out var serviceSub);

            serviceSub.FindParentContainer(
                ContainerRetrievalSetting.Override, container, parentContainer)
                .Returns((MockDependencyContainer)null);

            // Act
            LogAssert.ignoreFailingMessages = true;
            container.OnTransformParentChanged();
            LogAssert.ignoreFailingMessages = false;

            // Assert
            container.GetSerializedProperty("_overrideParent", out var overrideParent);

            Assert.That(overrideParent, Is.Null);
            serviceSub.Received(1).FindParentContainer(
                ContainerRetrievalSetting.Override, container, parentContainer);
        }


        [Test]
        [TestCase(ParentSetting.Global)]
        [TestCase(ParentSetting.HierarchyWithGlobal)]
        [TestCase(ParentSetting.HierarchyWithoutGlobal)]
        [TestCase(ParentSetting.Override)]
        public void OnValidate_VariousParentSettings_SetsToRetrievedContainer(
            ParentSetting parentSetting)
        {
            // Set up
            var container = SetUp.StandardContainer(parentSetting,
                out _, out var parentContainer, out var serviceSub);

            serviceSub.FindParentContainer(
                (ContainerRetrievalSetting)parentSetting, container, parentContainer)
                .Returns(parentContainer.InternalContainer);

            // Act
            container.OnValidate();

            // Assert
            Assert.That(container.InternalContainer.Parent,
                Is.EqualTo(parentContainer.InternalContainer));
            serviceSub.Received(1).FindParentContainer(
                (ContainerRetrievalSetting)parentSetting, container, parentContainer);
        }

        [Test]
        public void OnValidate_NoneParentSetting_SetsToNull()
        {
            // Set up
            var container = SetUp.StandardContainer(ParentSetting.None,
                out _, out var parentContainer, out var serviceSub);

            // Act
            container.OnValidate();

            // Assert
            Assert.That(container.InternalContainer.Parent, Is.Null);
            serviceSub.DidNotReceiveWithAnyArgs().FindParentContainer(
                ContainerRetrievalSetting.Global, container, parentContainer);
        }

        [Test]
        public void OnValidate_OverrideParentSettingWithInvalidOverride_SetsOverrideParentToNull()
        {
            // Set up
            var container = SetUp.StandardContainer(ParentSetting.Override,
                out _, out var parentContainer, out var serviceSub);

            serviceSub.FindParentContainer(
                ContainerRetrievalSetting.Override, container, parentContainer)
                .Returns((MockDependencyContainer)null);


            // Act
            LogAssert.ignoreFailingMessages = true;
            container.OnValidate();
            LogAssert.ignoreFailingMessages = false;

            // Assert
            container.GetSerializedProperty("_overrideParent", out var overrideParent);

            Assert.That(overrideParent, Is.Null);
            serviceSub.Received(1).FindParentContainer(
                ContainerRetrievalSetting.Override, container, parentContainer);
        }
    }
}