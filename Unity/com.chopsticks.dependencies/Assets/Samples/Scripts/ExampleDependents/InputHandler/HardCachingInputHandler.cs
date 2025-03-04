using Chopsticks.Dependencies;
using Chopsticks.Samples.ExampleDependencies.System;

namespace Chopsticks.Samples.ExampleDependents.InputHandler
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