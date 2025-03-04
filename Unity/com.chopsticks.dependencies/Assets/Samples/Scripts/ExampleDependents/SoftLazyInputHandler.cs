using Chopsticks.Dependencies;

namespace Chopsticks.Samples.ExampleDependents
{
    public class SoftLazyInputHandler : MonoDependent
    {
        private ISystem System
        {
            get
            {
                if (Resolve(out ISystem system))
                    return system;

                UnityEngine.Debug.LogWarning($"{nameof(SoftLazyInputHandler)} could not " +
                    $"resolve its {nameof(ISystem)} dependency. Dependent features " +
                    $"will be disabled.");

                return null;
            }

        }


        public void OnMouseUp()
        {
            System?.Perform();
        }
    }
}