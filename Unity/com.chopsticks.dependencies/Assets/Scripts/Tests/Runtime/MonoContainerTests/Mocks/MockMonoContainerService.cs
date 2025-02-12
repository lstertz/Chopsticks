using Chopsticks.Dependencies.Containers;
using Chopsticks.Dependencies.Services;
using NSubstitute;
using UnityEngine;

namespace MonoContainerTests.Mocks
{
    public class MockMonoContainerService : UnityContainerService<MockDependencyContainer,
        MockDependencyContainerFactory,
        MockDependencyContainer.Definition>
    {
        public IUnityContainerService<MockDependencyContainer, MockDependencyContainer.Definition> Sub { get; } =
            Substitute.For<IUnityContainerService<MockDependencyContainer, MockDependencyContainer.Definition>>();


        public override MockDependencyContainer FindParentContainer<TUnityContainer, TOverrideContainer>(
            ContainerRetrievalSetting setting, TUnityContainer unityContainer, 
            TOverrideContainer overrideContainer) =>
            Sub.FindParentContainer(setting, unityContainer, overrideContainer);

        public override MockDependencyContainer GetContainer<TOverrideContainer>(
            ContainerRetrievalSetting setting, bool includeSelf, Transform unityContainer,
            TOverrideContainer overrideContainer) => 
            Sub.GetContainer(setting, includeSelf, unityContainer, overrideContainer);
    }
}