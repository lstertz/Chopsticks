using Chopsticks.Dependencies.Containers;
using NSubstitute;

namespace IDependencyContainerExtensionsTests;

public class Resolve
{
    public static class Mock
    {
        public interface IContract { }

        public interface IInvalidContract { }
    }


    [Test]
    public void Resolve_InvalidContract_False()
    {
        // Set up
        var invalidImplementation = Substitute.For<Mock.IInvalidContract>();

        var container = Substitute.For<IDependencyContainer>();
        container.Resolve(typeof(Mock.IContract), out _).Returns(x =>
        {
            // Through non-generic typing, an implementation was registered for an invalid contract.
            x[1] = invalidImplementation;
            return true;
        });

        // Act
        bool wasResolved = container.Resolve<Mock.IContract>(out var implementation);

        // Assert
        container.Received(1).Resolve(typeof(Mock.IContract), out _);
        Assert.That(implementation, Is.Null);
        Assert.That(wasResolved, Is.False);
    }

    [Test]
    public void Resolve_UnknownDependency_False()
    {
        // Set up
        Mock.IContract? expectedImplementation = null;
        var isResolved = false;

        var container = Substitute.For<IDependencyContainer>();
        container.Resolve(typeof(Mock.IContract), out _).Returns(x =>
        {
            x[1] = expectedImplementation;
            return isResolved;
        });

        // Act
        bool wasResolved = container.Resolve<Mock.IContract>(out var implementation);

        // Assert
        container.Received(1).Resolve(typeof(Mock.IContract), out _);
        Assert.That(implementation, Is.EqualTo(expectedImplementation));
        Assert.That(wasResolved, Is.EqualTo(isResolved));
    }

    [Test]
    public void Resolve_ValidContract_CastsToGenericTyping()
    {
        // Set up
        var expectedImplementation = Substitute.For<Mock.IContract>();
        var isResolved = true;

        var container = Substitute.For<IDependencyContainer>();
        container.Resolve(typeof(Mock.IContract), out _).Returns(x =>
        {
            x[1] = expectedImplementation;
            return isResolved;
        });

        // Act
        bool wasResolved = container.Resolve<Mock.IContract>(out var implementation);

        // Assert
        container.Received(1).Resolve(typeof(Mock.IContract), out _);
        Assert.That(implementation, Is.EqualTo(expectedImplementation));
        Assert.That(wasResolved, Is.EqualTo(isResolved));
    }
}
