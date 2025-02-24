using Chopsticks.Dependencies.Containers;
using Chopsticks.Dependencies.Services;
using UnityEngine;

namespace Chopsticks.Dependencies.Initialization
{
    public class InitDependencies : MonoBehaviour
    {
        private void Awake()
        {
            var container = MonoContainerService.GlobalContainer;
            RegisterDependencies(container);

            // TODO :: Finalize registration and instantiate singletons.
        }

        protected virtual void RegisterDependencies(IDependencyContainer container)
        {

        }
    }
}
