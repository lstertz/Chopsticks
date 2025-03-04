using Chopsticks.Dependencies;

namespace Chopsticks.Samples.ExampleDependents
{
    public class HardCachingInputHandler : MonoDependent
    {
        private ISystem System => _system ??= AssertiveResolve<ISystem>();
        private ISystem _system;


        public void OnMouseUp()
        {
            System.Perform();
        }
    }
}