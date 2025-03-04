using Chopsticks.Dependencies.Containers;
using Chopsticks.Dependencies.Services;
using UnityEngine;


namespace Chopsticks.Samples.ExampleDependencies.System.Native
{
    public class NativeSystemLocalContainerWrapper : MonoBehaviour
    {
        private DependencyRegistration _registration;
        private MonoContainerService _containerService;

        private DependencyContainer _container;


        public void OnEnable()
        {
            _container = _containerService.GetContainerFromHierarchy(transform);
            _container.Register(new NativeSystem(), out _registration);
        }

        public void OnDisable()
        {
            _container.Deregister(_registration);
        }

        public void OnTransformParentChanged()
        {
            _container.Deregister(_registration);

            _container = _containerService.GetContainerFromHierarchy(transform);
            _container.Register(new NativeSystem(), out _registration);
        }
    }
}