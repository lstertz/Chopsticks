using Chopsticks.Dependencies.Containers;
using Chopsticks.Dependencies.Services;
using UnityEngine;


namespace Chopsticks.Samples.ExampleDependencies.ExampleSystem.Native
{
    /// <summary>
    /// An example wrapper of a native system, to enable registration of the system 
    /// with a container found within the hierarchy of the wrapper.
    /// </summary>
    public class NativeSystemLocalContainerWrapper : MonoBehaviour
    {
        /// <summary>
        /// The registration, maintained for runtime deregistration.
        /// </summary>
        /// <remarks>
        /// This is optional, as often global dependencies persist for the application's lifetime.
        /// </remarks>
        private DependencyRegistration _registration;

        /// <summary>
        /// The last known local container.
        /// </summary>
        private DependencyContainer _container;


        /// <summary>
        /// To align with the registration of <see cref="Dependencies.MonoDependency"/>, 
        /// registration is recommended to be done here in OnEnable.
        /// </summary>
        public void OnEnable()
        {
            _container = MonoContainerService.Shared.GetContainerFromHierarchy(transform);
            _container.Register<IExampleSystem>(new NativeSystem(), out _registration);
        }

        /// <summary>
        /// To mirror OnEnable, deregistration should be done in OnDisable, if at all.
        /// </summary>
        public void OnDisable()
        {
            _container.Deregister(_registration);
        }

        /// <summary>
        /// To ensure the container is updated properly for changes in the hierarchy, 
        /// OnTransformParentChanged should be implemented to deregister the dependency 
        /// from the previous container and register with the new container.
        /// </summary>
        /// <remarks>
        /// If the hierarchy for dependencies is expected to not change, 
        /// then this is not required.
        /// </remarks>
        public void OnTransformParentChanged()
        {
            _container.Deregister(_registration);

            _container = MonoContainerService.Shared.GetContainerFromHierarchy(transform);
            _container.Register(new NativeSystem(), out _registration);
        }
    }
}