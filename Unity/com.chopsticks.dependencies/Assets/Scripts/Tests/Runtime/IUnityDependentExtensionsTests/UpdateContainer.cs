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
using UnityEngine.SceneManagement;

namespace IUnityDependentExtensionsTests
{
    public class UpdateContainer
    {
        public static class SetUp
        {
            public static IUnityDependent<MockDependencyContainer, MockMonoContainerService> StandardDependent(
                ContainerRetrievalSetting containerSetting, MockDependencyContainer initialContainer,
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
                ContainerRetrievalSetting containerSetting, bool initContainerIsNull,
                out MockMonoDependent monoBehaviour,
                out IUnityContainer<MockDependencyContainer> overrideContainer,
                out MonoContainerService serviceSub)
            {
                var expectedContainer = Substitute.For<MockDependencyContainer>();
                var unityDependency = StandardDependent(containerSetting,
                    initContainerIsNull ? null : expectedContainer, 
                    true, out monoBehaviour, out overrideContainer, out serviceSub);

                serviceSub.GetContainer(
                    (ContainerSetting)containerSetting,
                    true,
                    monoBehaviour.transform,
                    overrideContainer).Returns(expectedContainer);

                return unityDependency;
            }
        }

        [SetUp]
        public void SetUpScene()
        {
            SceneManager.LoadScene("StandardTestScene");
        }


        [Test]
        [TestCase(ContainerRetrievalSetting.Global, true)]
        [TestCase(ContainerRetrievalSetting.Global, false)]
        [TestCase(ContainerRetrievalSetting.Hierarchy, true)]
        [TestCase(ContainerRetrievalSetting.Hierarchy, false)]
        [TestCase(ContainerRetrievalSetting.Override, true)]
        [TestCase(ContainerRetrievalSetting.Override, false)]
        public void SetContainer_AllContainerSettings_InvokesPostContainerChangedWhenContainerChanges(
            ContainerRetrievalSetting containerSetting, bool containerChanged)
        {
            // Set up
            var unityDependent = SetUp.ChangeableContainer(containerSetting,
                containerChanged, out var monoBehaviour,
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
        [TestCase(ContainerRetrievalSetting.Global, true)]
        [TestCase(ContainerRetrievalSetting.Global, false)]
        [TestCase(ContainerRetrievalSetting.Hierarchy, true)]
        [TestCase(ContainerRetrievalSetting.Hierarchy, false)]
        [TestCase(ContainerRetrievalSetting.Override, true)]
        [TestCase(ContainerRetrievalSetting.Override, false)]
        public void SetContainer_AllContainerSettings_InvokesPreContainerChangedWhenContainerChanges(
            ContainerRetrievalSetting containerSetting, bool containerChanged)
        {
            // Set up
            var unityDependent = SetUp.ChangeableContainer(containerSetting,
                containerChanged, out var monoBehaviour,
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
        [TestCase(ContainerRetrievalSetting.Global, true)]
        [TestCase(ContainerRetrievalSetting.Global, false)]
        [TestCase(ContainerRetrievalSetting.Hierarchy, true)]
        [TestCase(ContainerRetrievalSetting.Hierarchy, false)]
        [TestCase(ContainerRetrievalSetting.Override, true)]
        [TestCase(ContainerRetrievalSetting.Override, false)]
        public void UpdateContainer_VariousContainerSettings_SetsToServiceProvidedContainer(
            ContainerRetrievalSetting containerSetting, bool containerChanged)
        {
            // Set up
            var expectedContainer = Substitute.For<MockDependencyContainer>();
            var unityDependent = SetUp.StandardDependent(containerSetting,
                null, true, out var monoBehaviour, out var overrideContainer, out var serviceSub);

            serviceSub.GetContainer(
                (ContainerSetting)containerSetting,
                true,
                monoBehaviour.transform,
                overrideContainer).Returns(expectedContainer);

            // Act
            unityDependent.UpdateContainer(monoBehaviour, overrideContainer, null, null);

            // Assert
            Assert.That(unityDependent.Container, Is.EqualTo(expectedContainer));
            serviceSub.Received().GetContainer(
                (ContainerSetting)containerSetting,
                true,
                monoBehaviour.transform,
                overrideContainer);
        }
    }
}