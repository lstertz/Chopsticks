using Chopsticks.Dependencies.Containers;
using Chopsticks.Dependencies.Services;
using NSubstitute;
using UnityEngine;

namespace IUnityDependencyExtensionsTests.Mocks
{
    public class MockRegistrationContainerService : 
        IUnityContainerService<MockRegistrationDependencyContainer>
    {
        public IUnityContainerService<MockRegistrationDependencyContainer> Sub { get; } =
            Substitute.For<IUnityContainerService<MockRegistrationDependencyContainer>>();

        public MockRegistrationDependencyContainer FindParentContainer<TUnityContainer, TOverrideContainer>(
            ContainerRetrievalSetting setting, TUnityContainer unityContainer, TOverrideContainer overrideContainer)
            where TUnityContainer : MonoBehaviour, IUnityContainer<MockRegistrationDependencyContainer>
            where TOverrideContainer : IUnityContainer<MockRegistrationDependencyContainer> =>
                Sub.FindParentContainer(setting, unityContainer, overrideContainer);

        public MockRegistrationDependencyContainer GetContainer<TOverrideContainer>(
            ContainerRetrievalSetting setting, bool includeSelf, Transform unityContained, 
            TOverrideContainer overrideContainer)
            where TOverrideContainer : IUnityContainer<MockRegistrationDependencyContainer> => 
                Sub.GetContainer(setting, includeSelf, unityContained, overrideContainer);
    }
}