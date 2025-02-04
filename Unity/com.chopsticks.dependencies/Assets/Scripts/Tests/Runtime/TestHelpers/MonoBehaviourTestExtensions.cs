using UnityEditor;
using UnityEngine;

namespace TestHelpers
{
    public static class MonoBehaviourTestExtensions
    {
        public static MonoBehaviour GetSerializedProperty(
            this MonoBehaviour container,
            string propertyName, out Object value)
        {
            var serializedObject = new SerializedObject(container);
            var serializedProperty = serializedObject.FindProperty(propertyName);

            value = serializedProperty.objectReferenceValue;

            return container;
        }


        public static MonoBehaviour SetSerializedProperty(
            this MonoBehaviour container, 
            string propertyName, bool value)
        {
            var serializedObject = new SerializedObject(container);
            var serializedProperty = serializedObject.FindProperty(propertyName);

            serializedProperty.boolValue = value;
            serializedObject.ApplyModifiedProperties();

            return container;
        }
        public static MonoBehaviour SetSerializedProperty(
            this MonoBehaviour container,
            string propertyName, Chopsticks.Dependencies.Containers.ContainerParentSetting value)
        {
            var serializedObject = new SerializedObject(container);
            var serializedProperty = serializedObject.FindProperty(propertyName);

            serializedProperty.enumValueFlag = (int)value;
            serializedObject.ApplyModifiedProperties();

            return container;
        }

        public static MonoBehaviour SetSerializedProperty(
            this MonoBehaviour container,
            string propertyName, Object value)
        {
            var serializedObject = new SerializedObject(container);
            var serializedProperty = serializedObject.FindProperty(propertyName);

            serializedProperty.objectReferenceValue = value;
            serializedObject.ApplyModifiedProperties();

            return container;
        }
    }
}
