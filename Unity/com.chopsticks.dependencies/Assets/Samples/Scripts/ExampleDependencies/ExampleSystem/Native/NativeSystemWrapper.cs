using Chopsticks.Dependencies;

namespace Chopsticks.Samples.ExampleDependencies.ExampleSystem.Native
{
    /// <summary>
    /// An example wrapper of a native system, to enable registration of 
    /// a system that exists outside of Unity.
    /// </summary>
    public class NativeSystemWrapper : MonoDependencyWrapper
    {
        protected override void PerformRegistration()
        {
            Register<IExampleSystem>(new NativeSystem());
        }
    }
}