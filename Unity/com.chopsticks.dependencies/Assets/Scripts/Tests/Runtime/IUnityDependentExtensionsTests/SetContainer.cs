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
    public class SetContainer
    {
        public static class SetUp
        {
            public static IUnityDependent<MockDependencyContainer, MockMonoContainerService> StandardDependent(
                ContainerSetting containerSetting, out MockMonoDependent monoBehaviour,
                out IUnityContainer<MockDependencyContainer> overrideContainer,
                out MonoContainerService serviceSub)
            {
                GameObject gameObject = new GameObject();
                monoBehaviour = gameObject.AddComponent<MockMonoDependent>();
                monoBehaviour.SetSerializedProperty("_containerSetting", containerSetting);

                var unityDependent = monoBehaviour as IUnityDependent<MockDependencyContainer,
                    MockMonoContainerService>;
                unityDependent.Container = null;

                overrideContainer = Substitute.For<IUnityContainer<MockDependencyContainer>>();

                serviceSub = unityDependent.Service.Sub;
                serviceSub.ClearReceivedCalls();

                return unityDependent;
            }
        }


        [Test]
        public void SetContainer_NoneContainerSetting_ContainerSetToNull()
        {
            // Set up
            var unityDependent = SetUp.StandardDependent(ContainerSetting.None,
                out var monoBehaviour, out _, out var serviceSub);

            // Act
            unityDependent.SetContainer(monoBehaviour, null);

            // Assert
            Assert.That(unityDependent.Container, Is.Null);
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
            var unityDependent = SetUp.StandardDependent(containerSetting,
                out var monoBehaviour, out var overrideContainer, out var serviceSub);

            serviceSub.GetContainer(
                (ContainerRetrievalSetting)containerSetting,
                true,
                monoBehaviour.transform,
                overrideContainer).Returns(expectedContainer);

            // Act
            unityDependent.SetContainer(monoBehaviour, overrideContainer);

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