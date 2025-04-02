using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using Chopsticks.Dependencies.Containers;
using System.Collections.Generic;

namespace Chopsticks.Dependencies.Editor
{
    [CustomEditor(typeof(BaseMonoContainer<,,,>), true)]
    public class BaseMonoContainerEditor : UnityEditor.Editor
    {
        private const string ContainerParentSetting = "_containerParentSetting";
        private const string InheritParentDependencies = "_inheritParentDependencies";
        private const string OverrideParent = "_overrideParent";

        private static readonly HashSet<string> ExcludedProperties = new()
        {
            "m_Script",
            ContainerParentSetting,
            InheritParentDependencies,
            OverrideParent
        };


        private VisualElement _root;
        private VisualElement _configurationContainer;
        private PropertyField _parentSettingField;
        private PropertyField _inheritDependenciesField;
        private PropertyField _overrideParentField;
        private ObjectField _currentParentField;
        private Label _parentStateLabel;


        public override VisualElement CreateInspectorGUI()
        {
            var visualTree = LoadAsset<VisualTreeAsset>("BaseMonoContainerEditor.uxml");
            _root = visualTree.Instantiate();

            ApplyStylesheet();
            SetUpFieldReferences();
            SetUpConfigurationProperties();
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
            var parentSetting = (ContainerSetting)(serializedObject
                .FindProperty(ContainerParentSetting).enumValueIndex - 1);
            var overrideParent = (BaseUnityContainer)serializedObject
                .FindProperty(OverrideParent).objectReferenceValue;

            IUnityContainerEditor parentContainer = null;
            if (parentSetting != ContainerSetting.None)
                parentContainer = UnityEditorContainerService.FindParentUnityContainer(
                    (ContainerRetrievalSetting)parentSetting, (BaseUnityContainer)target, 
                    overrideParent);

            _currentParentField.value = parentContainer as MonoBehaviour;
            var hasParent = parentContainer != null;

            bool showObjectField = parentSetting == ContainerSetting.Override ||
                (parentSetting == ContainerSetting.HierarchyWithGlobal && hasParent) ||
                (parentSetting == ContainerSetting.HierarchyWithoutGlobal && hasParent);

            _currentParentField.style.display = showObjectField ? 
                DisplayStyle.Flex : DisplayStyle.None;
            _parentStateLabel.style.display = !showObjectField ? 
                DisplayStyle.Flex : DisplayStyle.None;

            if (!showObjectField)
            {
                string stateText = parentSetting switch
                {
                    ContainerSetting.Global => "Global",
                    ContainerSetting.HierarchyWithGlobal when !hasParent => "Global",
                    ContainerSetting.None => "None",
                    _ => "None (No Parent Found)"
                };
                _parentStateLabel.text = stateText;
            }
        }

        private void UpdateForParentSetting()
        {
            if (serializedObject == null)
                return;

            var parentSetting = (ContainerSetting)(serializedObject
                .FindProperty(ContainerParentSetting).enumValueIndex - 1);
            
            _inheritDependenciesField.style.display = parentSetting != ContainerSetting.None ? 
                DisplayStyle.Flex : DisplayStyle.None;
            _overrideParentField.style.display = parentSetting == ContainerSetting.Override ? 
                DisplayStyle.Flex : DisplayStyle.None;
        }


        private void ApplyStylesheet()
        {
            var styleSheet = LoadAsset<StyleSheet>("BaseMonoContainerEditor.uss");
            if (styleSheet != null)
                _root.styleSheets.Add(styleSheet);
        }

        private void BindFields()
        {
            _parentSettingField.BindProperty(serializedObject
                .FindProperty(ContainerParentSetting));
            _inheritDependenciesField.BindProperty(serializedObject
                .FindProperty(InheritParentDependencies));
            _overrideParentField.BindProperty(serializedObject
                .FindProperty(OverrideParent));
        }

        private void SetUpConfigurationProperties()
        {
            _configurationContainer = _root.Q<VisualElement>("configurationContainer");
            SerializedProperty property = serializedObject.GetIterator();
            property.NextVisible(true); // Jump into the script reference's properties.

            bool hasDrawnProperty = false;
            while (property.NextVisible(false))
            {
                if (ExcludedProperties.Contains(property.name))
                    continue;

                PropertyField field = new(property);
                _configurationContainer.Add(field);
                hasDrawnProperty = true;
            }

            if (!hasDrawnProperty)
                _configurationContainer.style.display = DisplayStyle.None;
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


        private T LoadAsset<T>(string assetFile)
            where T : Object
        {
            var visualTree = AssetDatabase.LoadAssetAtPath<T>(
                $"Packages/com.chopsticks.dependencies/Editor/Containers/{assetFile}");

            if (visualTree == null)  // Running in development package environment.
                visualTree = AssetDatabase.LoadAssetAtPath<T>(
                    $"Assets/Scripts/Editor/Containers/{assetFile}");

            return visualTree;
        }
    }
} 