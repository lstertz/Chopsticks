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
        }


        [Test]
        public void UpdateContainer_DisabledContained_DoesNothing()
        {
            // Set up
            var unityDependent = SetUp.StandardDependent(ContainerSetting.None, null, false,
                out var monoBehaviour, out _, out var serviceSub);

            // Act
            unityDependent.UpdateContainer(monoBehaviour, null, null);

            // Assert
            Assert.That(unityDependent.Container, Is.Null);
            serviceSub.DidNotReceiveWithAnyArgs().GetContainer(
                Arg.Any<ContainerRetrievalSetting>(),
                Arg.Any<bool>(),
                Arg.Any<Transform>(),
                Arg.Any<IUnityContainer<MockDependencyContainer>>());
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void UpdateContainer_NoneContainerSetting_ContainerSetToNull(
            bool containerChanged)
        {
            // Set up
            var unityDependent = SetUp.StandardDependent(ContainerSetting.None,
                !containerChanged ? null : Substitute.For<MockDependencyContainer>(), true,
                out var monoBehaviour, out var overrideContainer, out var serviceSub);

            bool calledContainerChanged = false;
            void onContainerChanged() => calledContainerChanged = true;

            // Act
            unityDependent.UpdateContainer(monoBehaviour, overrideContainer, onContainerChanged);

            // Assert
            Assert.That(unityDependent.Container, Is.Null);
            Assert.That(calledContainerChanged, Is.EqualTo(containerChanged));
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
                containerChanged ? null : expectedContainer, true,
                out var monoBehaviour, out var overrideContainer, out var serviceSub);

            serviceSub.GetContainer(
                (ContainerRetrievalSetting)containerSetting,
                true,
                monoBehaviour.transform,
                overrideContainer).Returns(expectedContainer);

            bool calledContainerChanged = false;
            void onContainerChanged() => calledContainerChanged = true;

            // Act
            unityDependent.UpdateContainer(monoBehaviour, overrideContainer, onContainerChanged);

            // Assert
            Assert.That(unityDependent.Container, Is.EqualTo(expectedContainer));
            Assert.That(calledContainerChanged, Is.EqualTo(containerChanged));
            serviceSub.Received().GetContainer(
                (ContainerRetrievalSetting)containerSetting,
                true,
                monoBehaviour.transform,
                overrideContainer);
        }
    }
}