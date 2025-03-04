using Chopsticks.Dependencies;
using UnityEngine;


namespace Chopsticks.Samples.ExampleDependencies.System.Output
{
    public class OutputSystem : MonoDependency, ISystem
    {
        [SerializeField]
        private string _configuredOutput;

        public void Perform()
        {
            Debug.Log(_configuredOutput);
        }


        protected override void OnRegistration()
        {
            RegisterAs<ISystem>();
        }
    }
}
