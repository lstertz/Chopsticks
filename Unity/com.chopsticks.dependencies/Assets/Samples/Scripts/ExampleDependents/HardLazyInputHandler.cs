using Chopsticks.Dependencies;

namespace Chopsticks.Samples.ExampleDependents
{
    public class HardLazyInputHandler : MonoDependent
    {
        private ISystem System => AssertiveResolve<ISystem>();


        public void OnMouseUp()
        {
            System.Perform();
        }
    }
}