using Chopsticks.Dependencies;
using Chopsticks.Samples.ExampleDependencies.System;

namespace Chopsticks.Samples.ExampleDependents.InputHandler
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