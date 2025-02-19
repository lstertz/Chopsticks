using Chopsticks.Dependencies.Contained;
using Chopsticks.Dependencies.Containers;
using MonoContainerTests.Mocks;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;
using TestHelpers;

using MonoContainerService = Chopsticks.Dependencies.Services.IUnityContainerService<
    MonoContainerTests.Mocks.MockDependencyContainer, 
    MonoContainerTests.Mocks.MockDependencyContainer.Definition>;
using IUnityDependentExtensionsTests.Mocks;

namespace IUnityDependentExtensionsTests
{
    public class UpdateContainer
    {
        public static class SetUp
        {
            public static IUnityDependent<MockDependencyContainer, MockMonoContainerService> StandardDependent(
                ContainerSetting containerSetting, MockDependencyContainer initialContainer,
                bool enabledGameObject, out MockMonoDependent monoBehaviour, 
                out IUnityContainer<MockDependencyContainer> overrideContainer,
                out MonoContainerService serviceSub)
            {
                GameObject gameObject = new GameObject();
                gameObject.SetActive(enabledGameObject);

                monoBehaviour = gameObject.AddComponent<MockMonoDependent>();
                monoBehaviour.SetSerializedProperty("_containerSetting", containerSetting);

                var unityDependent = monoBehaviour as IUnityDependent<MockDependencyContainer,
                    MockMonoContainerService>;
                unityDependent.Container = initialContainer;

                overrideContainer = Substitute.For<IUnityContainer<MockDependencyContainer>>();

                serviceSub = unityDependent.Service.Sub;
                serviceSub.ClearReceivedCalls();

                return unityDependent;
            }

            public static IUnityDependent<MockDependencyContainer, MockMonoContainerService> ChangeableContainer(
                ContainerSetting containerSetting, bool initContainerIsNull,
                out MockMonoDependent monoBehaviour,
                out IUnityContainer<MockDependencyContainer> overrideContainer,
                out MonoContainerService serviceSub)
            {
                var expectedContainer = Substitute.For<MockDependencyContainer>();
                var unityDependency = StandardDependent(containerSetting,
                    initContainerIsNull ? null : expectedContainer, 
                    true, out monoBehaviour, out overrideContainer, out serviceSub);

                serviceSub.GetContainer(
                    (ContainerRetrievalSetting)containerSetting,
                    true,
                    monoBehaviour.transform,
                    overrideContainer).Returns(expectedContainer);

                return unityDependency;
            }
        }


        [Test]
        [TestCase(ContainerSetting.None, true)]
        [TestCase(ContainerSetting.None, false)]
        [TestCase(ContainerSetting.Global, true)]
        [TestCase(ContainerSetting.Global, false)]
        [TestCase(ContainerSetting.HierarchyWithGlobal, true)]
        [TestCase(ContainerSetting.HierarchyWithGlobal, false)]
        [TestCase(ContainerSetting.HierarchyWithoutGlobal, true)]
        [TestCase(ContainerSetting.HierarchyWithoutGlobal, false)]
        [TestCase(ContainerSetting.Override, true)]
        [TestCase(ContainerSetting.Override, false)]
        public void SetContainer_AllContainerSettings_InvokesPostContainerChangedWhenContainerChanges(
            ContainerSetting containerSetting, bool containerChanged)
        {
            // Set up
            var initContainerIsNull = containerSetting != ContainerSetting.None ?
                containerChanged : !containerChanged;

            var unityDependent = SetUp.ChangeableContainer(containerSetting,
                initContainerIsNull, out var monoBehaviour,
                out var overrideContainer, out var serviceSub);

            bool calledOnPostContainerChanged = false;
            void onPostContainerChanged() => calledOnPostContainerChanged = true;

            // Act
            unityDependent.UpdateContainer(monoBehaviour, overrideContainer,
                null, onPostContainerChanged);

            // Assert
            Assert.That(calledOnPostContainerChanged, Is.EqualTo(containerChanged));
        }

        [Test]
        [TestCase(ContainerSetting.None, true)]
        [TestCase(ContainerSetting.None, false)]
        [TestCase(ContainerSetting.Global, true)]
        [TestCase(ContainerSetting.Global, false)]
        [TestCase(ContainerSetting.HierarchyWithGlobal, true)]
        [TestCase(ContainerSetting.HierarchyWithGlobal, false)]
        [TestCase(ContainerSetting.HierarchyWithoutGlobal, true)]
        [TestCase(ContainerSetting.HierarchyWithoutGlobal, false)]
        [TestCase(ContainerSetting.Override, true)]
        [TestCase(ContainerSetting.Override, false)]
        public void SetContainer_AllContainerSettings_InvokesPreContainerChangedWhenContainerChanges(
            ContainerSetting containerSetting, bool containerChanged)
        {
            // Set up
            var initContainerIsNull = containerSetting != ContainerSetting.None ?
                containerChanged : !containerChanged;

            var unityDependent = SetUp.ChangeableContainer(containerSetting,
                initContainerIsNull, out var monoBehaviour,
                out var overrideContainer, out var serviceSub);

            bool calledOnPreContainerChanged = false;
            void onPreContainerChanged() => calledOnPreContainerChanged = true;

            // Act
            unityDependent.UpdateContainer(monoBehaviour, overrideContainer, 
                onPreContainerChanged, null);

            // Assert
            Assert.That(calledOnPreContainerChanged, Is.EqualTo(containerChanged));
        }

        [Test]
        public void UpdateContainer_DisabledContained_DoesNothing()
        {
            // Set up
            var unityDependent = SetUp.StandardDependent(ContainerSetting.None, null, false,
                out var monoBehaviour, out _, out var serviceSub);

            // Act
            unityDependent.UpdateContainer(monoBehaviour, null, null, null);

            // Assert
            Assert.That(unityDependent.Container, Is.Null);
            serviceSub.DidNotReceiveWithAnyArgs().GetContainer(
                Arg.Any<ContainerRetrievalSetting>(),
                Arg.Any<bool>(),
                Arg.Any<Transform>(),
                Arg.Any<IUnityContainer<MockDependencyContainer>>());
        }

        [Test]
        public void UpdateContainer_NoneContainerSetting_ContainerSetToNull()
        {
            // Set up
            var unityDependent = SetUp.StandardDependent(ContainerSetting.None,
                Substitute.For<MockDependencyContainer>(), true,
                out var monoBehaviour, out var overrideContainer, out var serviceSub);

            // Act
            unityDependent.UpdateContainer(monoBehaviour, overrideContainer, null, null);

            // Assert
            Assert.That(unityDependent.Container, Is.Null);
            serviceSub.DidNotReceiveWithAnyArgs().GetContainer(
                Arg.Any<ContainerRetrievalSetting>(),
                Arg.Any<bool>(),
                Arg.Any<Transform>(),
                Arg.Any<IUnityContainer<MockDependencyContainer>>());
        }

        [Test]
        [TestCase(ContainerSetting.Global, true)]
        [TestCase(ContainerSetting.Global, false)]
        [TestCase(ContainerSetting.HierarchyWithGlobal, true)]
        [TestCase(ContainerSetting.HierarchyWithGlobal, false)]
        [TestCase(ContainerSetting.HierarchyWithoutGlobal, true)]
        [TestCase(ContainerSetting.HierarchyWithoutGlobal, false)]
        [TestCase(ContainerSetting.Override, true)]
        [TestCase(ContainerSetting.Override, false)]
        public void UpdateContainer_VariousContainerSettings_SetsToServiceProvidedContainer(
            ContainerSetting containerSetting, bool containerChanged)
        {
            // Set up
            var expectedContainer = Substitute.For<MockDependencyContainer>();
            var unityDependent = SetUp.StandardDependent(containerSetting,
                null, true, out var monoBehaviour, out var overrideContainer, out var serviceSub);

            serviceSub.GetContainer(
                (ContainerRetrievalSetting)containerSetting,
                true,
                monoBehaviour.transform,
                overrideContainer).Returns(expectedContainer);

            // Act
            unityDependent.UpdateContainer(monoBehaviour, overrideContainer, null, null);

            // Assert
            Assert.That(unityDependent.Container, Is.EqualTo(expectedContainer));
            serviceSub.Received().GetContainer(
                (ContainerRetrievalSetting)containerSetting,
                true,
                monoBehaviour.transform,
                overrideContainer);
        }
    }
}