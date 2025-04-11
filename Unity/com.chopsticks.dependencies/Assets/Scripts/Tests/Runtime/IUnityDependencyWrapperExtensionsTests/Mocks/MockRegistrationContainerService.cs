using Chopsticks.Dependencies.Containers;
using Chopsticks.Dependencies.Services;
using NSubstitute;
using UnityEngine;

namespace IUnityDependencyWrapperExtensionsTests.Mocks
{
    public class MockRegistrationContainerService : 
        IUnityContainerService<MockRegistrationDependencyContainer>
    {
        public IUnityContainerService<MockRegistrationDependencyContainer> Sub { get; } =
            Substitute.For<IUnityContainerService<MockRegistrationDependencyContainer>>();

        public MockRegistrationDependencyContainer FindParentContainer<TUnityContainer, TOverrideContainer>(
            ContainerSetting setting, TUnityContainer unityContainer, TOverrideContainer overrideContainer)
            where TUnityContainer : MonoBehaviour, IUnityContainer<MockRegistrationDependencyContainer>
            where TOverrideContainer : IUnityContainer<MockRegistrationDependencyContainer> =>
                Sub.FindParentContainer(setting, unityContainer, overrideContainer);

        public MockRegistrationDependencyContainer GetContainer<TOverrideContainer>(
            ContainerSetting setting, bool includeSelf, Transform unityContained, 
            TOverrideContainer overrideContainer)
            where TOverrideContainer : IUnityContainer<MockRegistrationDependencyContainer> => 
                Sub.GetContainer(setting, includeSelf, unityContained, overrideContainer);

        public MockRegistrationDependencyContainer GetContainerFromHierarchy(
            Transform unityContained, bool includeSelf = true, bool fallbackToGlobal = true) => 
            Sub.GetContainerFromHierarchy(unityContained, includeSelf, fallbackToGlobal);
    }
}