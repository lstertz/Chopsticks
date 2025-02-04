using Chopsticks.Dependencies.Containers;
using Chopsticks.Dependencies.Factories;
using Chopsticks.Dependencies.Resolutions;
using NSubstitute;

namespace DependencyContainerFactoryTests;

public class BuildContainer
{
    public static class Mock
    {
        public interface IContract { }
    }


    [Test]
    public void BuildResolutionFor_Contained_BuildsContainedResolution()
    {
        // Set up
        var implementation = Substitute.For<Mock.IContract>();
        var factory = new DependencyResolutionFactory();

        // Act
        var resolution = factory.BuildResolutionFor(new()
        { 
            Contract = typeof(Mock.IContract),
            ImplementationFactory = _ => implementation,
            Lifetime = Chopsticks.Dependencies.DependencyLifetime.Contained
        });

        // Assert
        Assert.That(resolution is ContainedResolution, Is.True);
        Assert.That(resolution.Registration.Contract, Is.EqualTo(typeof(Mock.IContract)));
        Assert.That(resolution.Get(Substitute.For<IDependencyContainer>()), 
            Is.EqualTo(implementation));
    }

    [Test]
    public void BuildResolutionFor_Singleton_BuildsSingletonResolution()
    {
        // Set up
        var implementation = Substitute.For<Mock.IContract>();
        var factory = new DependencyResolutionFactory();

        // Act
        var resolution = factory.BuildResolutionFor(new()
        {
            Contract = typeof(Mock.IContract),
            ImplementationFactory = _ => implementation,
            Lifetime = Chopsticks.Dependencies.DependencyLifetime.Singleton
        });

        // Assert
        Assert.That(resolution is SingletonResolution, Is.True);
        Assert.That(resolution.Registration.Contract, Is.EqualTo(typeof(Mock.IContract)));
        Assert.That(resolution.Get(Substitute.For<IDependencyContainer>()),
            Is.EqualTo(implementation));
    }

    [Test]
    public void BuildResolutionFor_Transient_BuildsTransientResolution()
    {
        // Set up
        var implementation = Substitute.For<Mock.IContract>();
        var factory = new DependencyResolutionFactory();

        // Act
        var resolution = factory.BuildResolutionFor(new()
        {
            Contract = typeof(Mock.IContract),
            ImplementationFactory = _ => implementation,
            Lifetime = Chopsticks.Dependencies.DependencyLifetime.Transient
        });

        // Assert
        Assert.That(resolution is TransientResolution, Is.True);
        Assert.That(resolution.Registration.Contract, Is.EqualTo(typeof(Mock.IContract)));
        Assert.That(resolution.Get(Substitute.For<IDependencyContainer>()),
            Is.EqualTo(implementation));
    }
}
