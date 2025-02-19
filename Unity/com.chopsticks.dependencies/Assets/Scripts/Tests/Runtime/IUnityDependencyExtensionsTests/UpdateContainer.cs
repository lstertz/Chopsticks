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
using IUnityDependencyExtensionsTests.Mocks;

namespace IUnityDependencyExtensionsTests
{
    public class UpdateContainer
    {
        public static class SetUp
        {
            public static IUnityDependency<MockDependencyContainer, MockMonoContainerService> StandardDependency(
                ContainerSetting containerSetting, MockDependencyContainer initialContainer,
                bool enabledGameObject, out MockMonoDependency monoBehaviour, 
                out IUnityContainer<MockDependencyContainer> overrideContainer,
                out MonoContainerService serviceSub)
            {
                GameObject gameObject = new GameObject();
                gameObject.SetActive(enabledGameObject);

                monoBehaviour = gameObject.AddComponent<MockMonoDependency>();
                monoBehaviour.SetSerializedProperty("_containerSetting", containerSetting);

                var unityDependency = monoBehaviour as IUnityDependency<MockDependencyContainer,
                    MockMonoContainerService>;
                unityDependency.Container = initialContainer;

                overrideContainer = Substitute.For<IUnityContainer<MockDependencyContainer>>();

                serviceSub = unityDependency.Service.Sub;
                serviceSub.ClearReceivedCalls();

                return unityDependency;
            }

            public static IUnityDependency<MockDependencyContainer, MockMonoContainerService> ChangeableContainer(
                ContainerSetting containerSetting, bool initContainerIsNull, 
                out MockMonoDependency monoBehaviour,
                out IUnityContainer<MockDependencyContainer> overrideContainer,
                out MonoContainerService serviceSub)
            {
                var expectedContainer = Substitute.For<MockDependencyContainer>();
                var unityDependency = StandardDependency(containerSetting,
                    initContainerIsNull ? null : expectedContainer, true,
                    out monoBehaviour, out overrideContainer, out serviceSub);

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
        public void SetContainer_AllContainerSettings_InvokesOnRegistrationWhenContainerChanges(
            ContainerSetting containerSetting, bool containerChanged)
        {
            // Set up
            var initContainerIsNull = containerSetting != ContainerSetting.None ?
                containerChanged : !containerChanged;

            var unityDependency = SetUp.ChangeableContainer(containerSetting,
                initContainerIsNull, out var monoBehaviour, 
                out var overrideContainer, out var serviceSub);

            bool calledOnRegistration = false;
            void onRegistration() => calledOnRegistration = true;

            // Act
            unityDependency.UpdateContainer(monoBehaviour, overrideContainer, 
                null, null, onRegistration);

            // Assert
            Assert.That(calledOnRegistration, Is.EqualTo(containerChanged));
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

            var unityDependency = SetUp.ChangeableContainer(containerSetting,
                initContainerIsNull, out var monoBehaviour,
                out var overrideContainer, out var serviceSub);

            bool calledOnPostContainerChanged = false;
            void onPostContainerChanged() => calledOnPostContainerChanged = true;

            // Act
            unityDependency.UpdateContainer(monoBehaviour, overrideContainer, 
                null, onPostContainerChanged, null);

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

            var unityDependency = SetUp.ChangeableContainer(containerSetting,
                initContainerIsNull, out var monoBehaviour,
                out var overrideContainer, out var serviceSub);

            bool calledOnPreContainerChanged = false;
            void onPreContainerChanged() => calledOnPreContainerChanged = true;

            // Act
            unityDependency.UpdateContainer(monoBehaviour, overrideContainer, 
                onPreContainerChanged, null, null);

            // Assert
            Assert.That(calledOnPreContainerChanged, Is.EqualTo(containerChanged));
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
        public void SetContainer_AllContainerSettings_PerformsDeregistrationWhenContainerChanges(
            ContainerSetting containerSetting, bool containerChanged)
        {
            // Set up
            var initContainerIsNull = containerSetting != ContainerSetting.None ?
                containerChanged : !containerChanged;

            var unityDependency = SetUp.ChangeableContainer(containerSetting,
                initContainerIsNull, out var monoBehaviour,
                out var overrideContainer, out var serviceSub);

            // Act
            unityDependency.UpdateContainer(monoBehaviour, overrideContainer, 
                null, null, null);

            // Assert
            Assert.Ignore();
        }

        [Test]
        public void UpdateContainer_DisabledContained_DoesNothing()
        {
            // Set up
            var unityDependent = SetUp.StandardDependency(ContainerSetting.None, null, false,
                out var monoBehaviour, out _, out var serviceSub);

            // Act
            unityDependent.UpdateContainer(monoBehaviour, null, null, null, null);

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
            var unityDependency = SetUp.StandardDependency(ContainerSetting.None,
                Substitute.For<MockDependencyContainer>(), true, out var monoBehaviour,
                out var overrideContainer, out var serviceSub);

            // Act
            unityDependency.UpdateContainer(monoBehaviour, overrideContainer, 
                null, null, null);

            // Assert
            Assert.That(unityDependency.Container, Is.Null);
            serviceSub.DidNotReceiveWithAnyArgs().GetContainer(
                Arg.Any<ContainerRetrievalSetting>(),
                Arg.Any<bool>(),
                Arg.Any<Transform>(),
                Arg.Any<IUnityContainer<MockDependencyContainer>>());
        }

        [Test]
        [TestCase(ContainerSetting.Global)]
        [TestCase(ContainerSetting.HierarchyWithGlobal)]
        [TestCase(ContainerSetting.HierarchyWithoutGlobal)]
        [TestCase(ContainerSetting.Override)]
        public void UpdateContainer_VariousContainerSettings_SetsToServiceProvidedContainer(
            ContainerSetting containerSetting)
        {
            // Set up
            var expectedContainer = Substitute.For<MockDependencyContainer>();
            var unityDependency = SetUp.StandardDependency(containerSetting,
                null, true, out var monoBehaviour, out var overrideContainer, out var serviceSub);

            serviceSub.GetContainer(
                (ContainerRetrievalSetting)containerSetting,
                true,
                monoBehaviour.transform,
                overrideContainer).Returns(expectedContainer);

            // Act
            unityDependency.UpdateContainer(monoBehaviour, overrideContainer,
                null, null, null);

            // Assert
            Assert.That(unityDependency.Container, Is.EqualTo(expectedContainer));
            serviceSub.Received().GetContainer(
                (ContainerRetrievalSetting)containerSetting,
                true,
                monoBehaviour.transform,
                overrideContainer);
        }
    }
}