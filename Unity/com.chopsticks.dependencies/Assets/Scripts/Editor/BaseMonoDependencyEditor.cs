using UnityEditor;

namespace Chopsticks.Dependencies.Editor
{
    [CustomEditor(typeof(BaseMonoDependency<,>), true)]
    public class BaseMonoDependencyEditor : BaseMonoDependentEditor
    {
        /// <inheritdoc/>
        protected override string Title => "Chopsticks Dependency";
    }
} 