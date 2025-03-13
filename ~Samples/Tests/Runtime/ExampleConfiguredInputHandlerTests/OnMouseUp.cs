using Chopsticks.Dependencies.Contained;
using Chopsticks.Dependencies.Containers;
using Chopsticks.Samples.ExampleDependencies.ExampleSystem;
using Chopsticks.Samples.ExampleDependents.InputHandler;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;

namespace ExampleConfiguredInputHandlerTests
{
    /// <summary>
    /// An example test class to verify <see cref="ConfiguredInputHandler.OnMouseUp"/>, 
    /// showing how to leverage the dependency container to mock dependencies under test.
    /// </summary>
    public class OnMouseUp
    {
        /// <summary>
        /// A test case that verifies that the <see cref="IExampleSystem"/> depended upon by 
        /// <see cref="ConfiguredInputHandler"/> is used as expected upon 
        /// <see cref="ConfiguredInputHandler.OnMouseUp"/>.
        /// </summary>
        [Test]
        public void OnMouseUp_System_Performs()
        {
            // Set up - Define the dependent and set its dependencies as mocks.
            // All implementations of IMonoDependency/IMonoDependent and 
            // subclasses of MonoDependency/MonoDependent can be set up similarly.

            // Create and set up mock dependencies.
            var system = Substitute.For<IExampleSystem>();

            // Create a container specific to the test conditions.
            var container = new DependencyContainer();
            container.Register(system);

            // Create and set up the GameObject.
            var gameObject = new GameObject();
            gameObject.SetActive(false); // Disable to prevent unrelated functionality.

            // Create the input handler and set its container to define its test state.
            var inputHandler = gameObject.AddComponent<ConfiguredInputHandler>();
            (inputHandler as IUnityContained<DependencyContainer>).Container = container;


            // Act - Initiate the method under test.
            inputHandler.OnMouseUp();


            // Assert - Verify dependencies are used as expected.
            system.Received(1).Perform();


            // Tear down - Destroy the GameObject to clear for the next test.
            Object.DestroyImmediate(gameObject);
        }
    }
}