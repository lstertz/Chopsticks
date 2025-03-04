using Chopsticks.Dependencies;

namespace Chopsticks.Samples.ExampleDependents
{
    public class SoftCachingInputHandler : MonoDependent
    {
        private ISystem _system;


        protected override void OnPostContainerChanged()
        {
            if (!Resolve(out _system))
                UnityEngine.Debug.LogWarning($"{nameof(SoftCachingInputHandler)} could not " +
                    $"resolve its {nameof(ISystem)} dependency. Dependent features " +
                    $"will be disabled.");
        }

        public void OnMouseUp()
        {
            _system?.Perform();
        }
    }
}