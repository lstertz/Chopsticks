using Chopsticks.Dependencies.Containers;
using Chopsticks.Dependencies.Factories;
using Chopsticks.Dependencies.Resolutions;
using NSubstitute;

namespace DefaultDependencyContainerFactoryTests;

public class BuildResolutionFor
{
    public static class Mock
    {
        public interface IContract { }
    }


    [Test]
    public void BuildContainer_FirstCall_BuildsNewInstance()
    {
        // Set up
        var factory = new DefaultDependencyContainerFactory();

        // Act
        var container = factory.BuildContainer();

        // Assert
        Assert.That(container, Is.Not.Null);
        Assert.That(container is DependencyContainer, Is.True);
    }

    [Test]
    public void BuildContainer_SecondCall_BuildsNewInstance()
    {
        // Set up
        var factory = new DefaultDependencyContainerFactory();
        var firstContainer = factory.BuildContainer();

        // Act
        var secondContainer = factory.BuildContainer();

        // Assert
        Assert.That(secondContainer, Is.Not.EqualTo(firstContainer));
    }

    [Test]
    public void BuildContainer_WithDefinition_AppliesResolutionFactory()
    {
        // Set up
        var containerFactory = new DefaultDependencyContainerFactory();
        Func<IDependencyContainer, string> implementationFactory = 
            _ => "String Dependency to verify resolution factory";
        var dependencySpec = new DependencySpecification()
        {
            Contract = typeof(string),
            ImplementationFactory = implementationFactory
        };

        var resolutionFactory = Substitute.For<IDependencyResolutionFactory>();
        resolutionFactory.BuildResolutionFor(dependencySpec).Returns(
            new SingletonResolution(typeof(string), implementationFactory));

        // Act
        var container = containerFactory.BuildContainer(new()
        {
            ResolutionFactory = resolutionFactory
        });
        container.Register(dependencySpec, out _);

        // Assert
        Assert.That(container, Is.Not.Null);
        Assert.That(container is DependencyContainer, Is.True);

        resolutionFactory.Received(1).BuildResolutionFor(dependencySpec);
    }
}
