using MonoContainerTests.Mocks;
using NUnit.Framework;
using TestHelpers;
using UnityEngine;

namespace MonoContainerTests
{
    public class InheritParentDependencies
    {
        [Test]
        public void InheritParentDependencies_FalseThroughSerializedFieldOnAwake_Sets()
        {
            // Set up
            var gameObject = new GameObject();
            gameObject.SetActive(false);

            var container = gameObject.AddComponent<MockMonoContainer>();
            container.SetSerializedProperty("_inheritParentDependencies", false);

            // Act
            gameObject.SetActive(true);

            // Assert
            Assert.That(container.InternalContainer.InheritParentDependencies, Is.False);
        }

        [Test]
        public void InheritParentDependencies_TrueThroughSerializedFieldOnAwake_Sets()
        {
            // Set up
            var gameObject = new GameObject();
            gameObject.SetActive(false);

            var container = gameObject.AddComponent<MockMonoContainer>();
            container.SetSerializedProperty("_inheritParentDependencies", true);

            // Act
            gameObject.SetActive(true);

            // Assert
            Assert.That(container.InternalContainer.InheritParentDependencies, Is.True);
        }


        // TODO :: Test for changes after creation.
    }
}