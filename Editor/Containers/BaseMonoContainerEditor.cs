using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using Chopsticks.Dependencies.Containers;

namespace Chopsticks.Dependencies.Editor
{
    [CustomEditor(typeof(BaseMonoContainer<,,,>), true)]
    public class BaseMonoContainerEditor : UnityEditor.Editor
    {
        private VisualElement _root;
        private PropertyField _parentSettingField;
        private PropertyField _inheritDependenciesField;
        private PropertyField _overrideParentField;
        private ObjectField _currentParentField;
        private Label _parentStateLabel;


        public override VisualElement CreateInspectorGUI()
        {
            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(
                "Assets/Scripts/Editor/Containers/BaseMonoContainerEditor.uxml");
            _root = visualTree.Instantiate();

            ApplyStylesheet();
            SetUpFieldReferences();
            BindFields();

            Refresh();
            EditorApplication.update += Refresh;

            return _root;
        }

        public void OnDestroy() => 
            EditorApplication.update -= Refresh;


        private void Refresh()
        {
            // Prevent containers from being disabled, since that isn't supported.
            if (target != null && !((MonoBehaviour)target).enabled)
                ((MonoBehaviour)target).enabled = true;

            UpdateForParentSetting();
            UpdateCurrentParentContainer();
        }
        

        private void UpdateCurrentParentContainer()
        {
            var parentSetting = (ContainerParentSetting)(serializedObject
                .FindProperty("_containerParentSetting").enumValueIndex - 1);
            var overrideParent = (BaseUnityContainer)serializedObject
                .FindProperty("_overrideParent").objectReferenceValue;

            IUnityContainerEditor parentContainer = null;
            if (parentSetting != ContainerParentSetting.None)
                parentContainer = UnityEditorContainerService.FindParentUnityContainer(
                    (ContainerRetrievalSetting)parentSetting, (BaseUnityContainer)target, 
                    overrideParent);

            _currentParentField.value = parentContainer as MonoBehaviour;
            var hasParent = parentContainer != null;

            bool showObjectField = parentSetting == ContainerParentSetting.Override ||
                (parentSetting == ContainerParentSetting.HierarchyWithGlobal && hasParent) ||
                (parentSetting == ContainerParentSetting.HierarchyWithoutGlobal && hasParent);

            _currentParentField.style.display = showObjectField ? 
                DisplayStyle.Flex : DisplayStyle.None;
            _parentStateLabel.style.display = !showObjectField ? 
                DisplayStyle.Flex : DisplayStyle.None;

            if (!showObjectField)
            {
                string stateText = parentSetting switch
                {
                    ContainerParentSetting.Global => "Global",
                    ContainerParentSetting.HierarchyWithGlobal when !hasParent => "Global",
                    ContainerParentSetting.None => "None",
                    _ => "None (No Parent Found)"
                };
                _parentStateLabel.text = stateText;
            }
        }

        private void UpdateForParentSetting()
        {
            if (serializedObject == null)
                return;

            var parentSetting = (ContainerParentSetting)(serializedObject
                .FindProperty("_containerParentSetting").enumValueIndex - 1);
            
            _inheritDependenciesField.style.display = parentSetting != ContainerParentSetting.None ? 
                DisplayStyle.Flex : DisplayStyle.None;
            _overrideParentField.style.display = parentSetting == ContainerParentSetting.Override ? 
                DisplayStyle.Flex : DisplayStyle.None;
        }


        private void ApplyStylesheet()
        {
            var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>(
                "Assets/Scripts/Editor/Containers/BaseMonoContainerEditor.uss");
            if (styleSheet != null)
                _root.styleSheets.Add(styleSheet);
        }

        private void BindFields()
        {
            _parentSettingField.BindProperty(serializedObject.FindProperty("_containerParentSetting"));
            _inheritDependenciesField.BindProperty(serializedObject.FindProperty("_inheritParentDependencies"));
            _overrideParentField.BindProperty(serializedObject.FindProperty("_overrideParent"));
        }

        private void SetUpFieldReferences()
        {
            _parentSettingField = _root.Q<PropertyField>("parentSetting");
            _inheritDependenciesField = _root.Q<PropertyField>("inheritDependencies");
            _overrideParentField = _root.Q<PropertyField>("overrideParent");
            
            _parentStateLabel = _root.Q<Label>("parentStateLabel");
            _currentParentField = _root.Q<ObjectField>("currentParent");
            _currentParentField.objectType = typeof(BaseUnityContainer);
            _currentParentField.SetEnabled(false);
        }
    }
} 