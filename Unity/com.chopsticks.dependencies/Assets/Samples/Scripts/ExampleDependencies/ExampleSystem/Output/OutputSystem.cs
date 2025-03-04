using Chopsticks.Dependencies;
using UnityEngine;


namespace Chopsticks.Samples.ExampleDependencies.ExampleSystem.Output
{
    public class OutputSystem : MonoDependency, IExampleSystem
    {
        [SerializeField]
        private string _configuredOutput;

        public void Perform()
        {
            Debug.Log(_configuredOutput);
        }


        protected override void OnRegistration()
        {
            RegisterAs<IExampleSystem>();
        }
    }
}
