using Chopsticks.Dependencies.Containers;
using Chopsticks.Dependencies.Services;
using UnityEngine;


namespace Chopsticks.Samples.ExampleDependencies.System.Native
{
    public class NativeSystemGlobalContainerWrapper : MonoBehaviour
    {
        private DependencyRegistration _registration;


        public void OnEnable()
        {
            MonoContainerService.GlobalContainer.Register(
                new NativeSystem(), out _registration);
        }

        public void OnDisable()
        {
            MonoContainerService.GlobalContainer.Deregister(_registration);
        }
    }
}