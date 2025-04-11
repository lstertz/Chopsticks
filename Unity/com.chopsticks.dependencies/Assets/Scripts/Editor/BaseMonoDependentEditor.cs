using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEditor.UIElements;
using Chopsticks.Dependencies.Containers;
using System.Collections.Generic;

namespace Chopsticks.Dependencies.Editor
{
    [CustomEditor(typeof(BaseMonoDependent<,>), true)]
    public class BaseMonoDependentEditor : UnityEditor.Editor
    {
        private const string ContainerSetting = "_containerSetting";
        private const string OverrideContainer = "_overrideContainer";

        private static readonly HashSet<string> ExcludedProperties = new()
        {
            ContainerSetting,
            OverrideContainer
        };


        protected virtual string Title => "Chopsticks Dependent";

        private VisualElement _root;

        private VisualElement _configurationContainer;
        private PropertyField _containerSettingField;
        private ObjectField _currentParentField;
        private PropertyField _overrideContainerField;
        private Label _parentStateLabel;
        private Label _titleLabel;


        public override VisualElement CreateInspectorGUI()
        {
            var visualTree = LoadAsset<VisualTreeAsset>("BaseMonoDependentEditor.uxml");
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
            var containerSetting = (ContainerRetrievalSetting)serializedObject
                .FindProperty(ContainerSetting).enumValueIndex;
            var overrideParent = (BaseUnityContainer)serializedObject
                .FindProperty(OverrideContainer).objectReferenceValue;

            MonoBehaviour parentContainer = 
                UnityEditorContainerService.FindParentUnityContainer
                <BaseUnityContainer, BaseUnityContainer>(
                    (ContainerSetting)containerSetting, (MonoBehaviour)target, 
                    overrideParent);

            _currentParentField.value = parentContainer;
            var hasParent = parentContainer != null;

            bool showObjectField = containerSetting == ContainerRetrievalSetting.Override ||
                containerSetting == ContainerRetrievalSetting.Hierarchy && hasParent;

            _currentParentField.style.display = showObjectField ? 
                DisplayStyle.Flex : DisplayStyle.None;
            _parentStateLabel.style.display = !showObjectField ? 
                DisplayStyle.Flex : DisplayStyle.None;

            if (!showObjectField)
            {
                string stateText = containerSetting switch
                {
                    ContainerRetrievalSetting.Global => "Global",
                    ContainerRetrievalSetting.Hierarchy when !hasParent => "Global",
                    _ => "None (No Parent Found)"
                };
                _parentStateLabel.text = stateText;
            }
        }

        private void UpdateForParentSetting()
        {
            if (serializedObject == null)
                return;

            var containerSetting = (ContainerRetrievalSetting)serializedObject
                .FindProperty(ContainerSetting).enumValueIndex;

            _overrideContainerField.style.display = 
                containerSetting == ContainerRetrievalSetting.Override ?
                DisplayStyle.Flex : DisplayStyle.None;
        }


        private void ApplyStylesheet()
        {
            var styleSheet = LoadAsset<StyleSheet>("BaseMonoDependentEditor.uss");
            if (styleSheet != null)
                _root.styleSheets.Add(styleSheet);
        }

        private void BindFields()
        {
            _containerSettingField.BindProperty(serializedObject
                .FindProperty(ContainerSetting));
            _overrideContainerField.BindProperty(serializedObject
                .FindProperty(OverrideContainer));
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
            _containerSettingField = _root.Q<PropertyField>("containerSetting");
            _overrideContainerField = _root.Q<PropertyField>("overrideContainer");

            _titleLabel = _root.Q<Label>("titleLabel");
            _titleLabel.text = Title;
            _parentStateLabel = _root.Q<Label>("containerStateLabel");

            _currentParentField = _root.Q<ObjectField>("currentContainer");
            _currentParentField.objectType = typeof(BaseUnityContainer);
            _currentParentField.SetEnabled(false);
        }


        private T LoadAsset<T>(string assetFile)
            where T : Object
        {
            var visualTree = AssetDatabase.LoadAssetAtPath<T>(
                $"Packages/com.chopsticks.dependencies/Editor/{assetFile}");

            if (visualTree == null)  // Running in development package environment.
                visualTree = AssetDatabase.LoadAssetAtPath<T>(
                    $"Assets/Scripts/Editor/{assetFile}");

            return visualTree;
        }
    }
} 