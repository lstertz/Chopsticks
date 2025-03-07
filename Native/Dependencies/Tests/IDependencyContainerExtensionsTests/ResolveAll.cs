using Chopsticks.Dependencies.Containers;
using NSubstitute;

namespace IDependencyContainerExtensionsTests;

public class ResolveAll
{
    public static class Mock
    {
        public interface IContract { }

        public interface IInvalidContract { }
    }


    [Test]
    public void ResolveAll_SomeValidSomeInvalid_IncludesValidlyCast()
    {
        // Set up
        var implementationA = Substitute.For<Mock.IContract>();
        var implementationB = Substitute.For<Mock.IInvalidContract>();
        var implementationC = Substitute.For<Mock.IContract>();
        var implementationD = Substitute.For<Mock.IInvalidContract>();
        IEnumerable<object> allImplementations = [
            implementationA,
            implementationB,  // Registered non-generically where actual typing isn't enforced.
            implementationC,
            implementationD   // Registered non-generically where actual typing isn't enforced.
            ];
        IEnumerable<object> expectedImplementations = [
            implementationA,
            implementationC
            ];

        var container = Substitute.For<IDependencyContainer>();
        container.ResolveAll(typeof(Mock.IContract)).Returns(allImplementations);

        // Act
        var implementations = container.ResolveAll<Mock.IContract>().ToArray();

        // Assert
        container.Received(1).ResolveAll(typeof(Mock.IContract));
        Assert.That(implementations, Is.EquivalentTo(expectedImplementations));
    }
}
