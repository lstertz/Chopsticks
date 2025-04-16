using UnityEditor;

namespace Chopsticks.Dependencies.Editor
{
    [CustomEditor(typeof(BaseMonoDependencyWrapper<,>), true)]
    public class BaseMonoDependencyWrapperEditor : BaseMonoDependentEditor
    {
        // Maintain through the editor title that a wrapper is the same as a real dependency.
        /// <inheritdoc/>
        protected override string Title => "Chopsticks Dependency";
    }
} 