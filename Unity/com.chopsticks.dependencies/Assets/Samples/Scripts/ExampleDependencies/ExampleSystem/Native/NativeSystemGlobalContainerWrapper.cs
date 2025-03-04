using Chopsticks.Dependencies.Containers;
using Chopsticks.Dependencies.Services;
using UnityEngine;


namespace Chopsticks.Samples.ExampleDependencies.ExampleSystem.Native
{
    /// <summary>
    /// An example wrapper of a native system, to enable registration of the system 
    /// with the global container.
    /// </summary>
    public class NativeSystemGlobalContainerWrapper : MonoBehaviour
    {
        /// <summary>
        /// The registration, maintained for runtime deregistration.
        /// </summary>
        /// <remarks>
        /// This is optional, as often global dependencies persist for the application's lifetime.
        /// </remarks>
        private DependencyRegistration _registration;


        /// <summary>
        /// To align with the registration of <see cref="Dependencies.MonoDependency"/>, 
        /// registration is recommended to be done here in OnEnable.
        /// </summary>
        public void OnEnable()
        {
            MonoContainerService.GlobalContainer.Register<IExampleSystem>(
                new NativeSystem(), out _registration);
        }

        /// <summary>
        /// To mirror OnEnable, deregistration should be done in OnDisable, if at all.
        /// </summary>
        public void OnDisable()
        {
            MonoContainerService.GlobalContainer.Deregister(_registration);
        }
    }
}