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
    public class SetContainer
    {
        public static class SetUp
        {
            public static IUnityDependency<MockDependencyContainer, MockMonoContainerService> StandardDependency(
                ContainerSetting containerSetting, out MockMonoDependency monoBehaviour,
                out IUnityContainer<MockDependencyContainer> overrideContainer,
                out MonoContainerService serviceSub)
            {
                GameObject gameObject = new GameObject();
                monoBehaviour = gameObject.AddComponent<MockMonoDependency>();
                monoBehaviour.SetSerializedProperty("_containerSetting", containerSetting);

                var unityDependency = monoBehaviour as IUnityDependency<MockDependencyContainer,
                    MockMonoContainerService>;
                unityDependency.Container = null;

                overrideContainer = Substitute.For<IUnityContainer<MockDependencyContainer>>();

                serviceSub = unityDependency.Service.Sub;
                serviceSub.ClearReceivedCalls();

                return unityDependency;
            }
        }


        [Test]
        [TestCase(ContainerSetting.None)]
        [TestCase(ContainerSetting.Global)]
        [TestCase(ContainerSetting.HierarchyWithGlobal)]
        [TestCase(ContainerSetting.HierarchyWithoutGlobal)]
        [TestCase(ContainerSetting.Override)]
        public void SetContainer_AllContainerSettings_InvokesOnRegistration(
            ContainerSetting containerSetting)
        {
            // Set up
            var unityDependency = SetUp.StandardDependency(containerSetting,
                out var monoBehaviour, out _, out var serviceSub);

            bool calledOnRegistration = false;
            void onRegistration() => calledOnRegistration = true;

            // Act
            unityDependency.SetContainer(monoBehaviour, null, onRegistration);

            // Assert
            Assert.That(calledOnRegistration, Is.True);
        }

        [Test]
        public void SetContainer_NoneContainerSetting_ContainerSetToNull()
        {
            // Set up
            var unityDependency = SetUp.StandardDependency(ContainerSetting.None,
                out var monoBehaviour, out _, out var serviceSub);

            // Act
            unityDependency.SetContainer(monoBehaviour, null, null);

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
        public void SetContainer_VariousContainerSettings_SetsToServiceProvidedContainer(
            ContainerSetting containerSetting)
        {
            // Set up
            var expectedContainer = Substitute.For<MockDependencyContainer>();
            var unityDependency = SetUp.StandardDependency(containerSetting,
                out var monoBehaviour, out var overrideContainer, out var serviceSub);

            serviceSub.GetContainer(
                (ContainerRetrievalSetting)containerSetting,
                true,
                monoBehaviour.transform,
                overrideContainer).Returns(expectedContainer);

            // Act
            unityDependency.SetContainer(monoBehaviour, overrideContainer, null);

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