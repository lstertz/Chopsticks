using Chopsticks.Dependencies.Factories;
using NSubstitute;

namespace MonoContainerTests.Mocks
{
    public class MockDependencyContainerFactory : 
        IDependencyContainerFactory<MockDependencyContainer, MockDependencyContainer.Definition>
    {
        public static class Received
        {
            public static Call<MockDependencyContainer.Definition> BuildContainerCall { get; set; }
        }

        public struct Call<T>
        {
            public T Parameter { get; set; }
            public bool WasCalled { get; set; }
        }


        public static void ResetMockState()
        {
            Received.BuildContainerCall = new();
        }


        public MockDependencyContainer BuildContainer(MockDependencyContainer.Definition def)
        {
            Received.BuildContainerCall = new()
            {
                Parameter = def,
                WasCalled = true
            };

            return Substitute.For<MockDependencyContainer>();
        }
    }
}