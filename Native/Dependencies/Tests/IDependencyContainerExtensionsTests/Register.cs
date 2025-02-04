using Chopsticks.Dependencies;
using Chopsticks.Dependencies.Containers;
using NSubstitute;

namespace IDependencyContainerExtensionsTests;

public class Register
{
    public static class Mock
    {
        public static DependencyLifetime RandomLifetime => 
            (DependencyLifetime)Random.Shared.Next(3);

        public interface IContract { }

        public class DependencyContainer : IDependencyContainer
        {
            public (DependencySpecification Spec, DependencyRegistration Registration) CallResults 
            { get; set; }

            IDependencyContainer IDependencyContainer.Deregister(
                DependencyRegistration registration)
            {
                throw new NotImplementedException();
            }

            IDependencyContainer IDependencyContainer.Register(
                DependencySpecification specification, 
                out DependencyRegistration registration)
            {
                registration = new()
                { 
                    Contract = specification.Contract 
                };
                CallResults = (specification, registration);

                return this;
            }

            bool IDependencyContainer.Resolve(Type contract, out object? implementation)
            {
                throw new NotImplementedException();
            }

            IEnumerable<object> IDependencyContainer.ResolveAll(Type contract)
            {
                throw new NotImplementedException();
            }
        }
    }


    [Test]
    public void Register_ForDependencyFactoryWithOut_CallsWithSpecAndPropagatesOut()
    {
        // Set up
        var lifetime = Mock.RandomLifetime;
        var container = new Mock.DependencyContainer();
        var factoryResult = Substitute.For<Mock.IContract>();
        Mock.IContract Factory(IDependencyContainer d) => factoryResult;

        // Act
        container.Register(Factory, out var registration, lifetime);

        // Assert
        Assert.That(container.CallResults.Spec.Contract, Is.EqualTo(typeof(Mock.IContract)));
        Assert.That(container.CallResults.Spec.Lifetime, Is.EqualTo(lifetime));
        Assert.That(container.CallResults.Spec.ImplementationFactory(container),
            Is.EqualTo(factoryResult));

        Assert.That(registration, Is.EqualTo(container.CallResults.Registration));
    }

    [Test]
    public void Register_ForDependencyFactoryWithoutOut_CallsWithSpecAndIgnoresOut()
    {
        // Set up
        var lifetime = Mock.RandomLifetime;
        var container = new Mock.DependencyContainer();
        var factoryResult = Substitute.For<Mock.IContract>();
        Mock.IContract Factory(IDependencyContainer d) => factoryResult;

        // Act
        container.Register(Factory, lifetime);

        // Assert
        Assert.That(container.CallResults.Spec.Contract, Is.EqualTo(typeof(Mock.IContract)));
        Assert.That(container.CallResults.Spec.Lifetime, Is.EqualTo(lifetime));
        Assert.That(container.CallResults.Spec.ImplementationFactory(container),
            Is.EqualTo(factoryResult));
    }

    [Test]
    public void Register_ForDependencyInstanceWithOut_CallsWithSpecAndPropagatesOut()
    {
        // Set up
        var lifetime = Mock.RandomLifetime;
        var container = new Mock.DependencyContainer();
        var dependency = Substitute.For<Mock.IContract>();

        // Act
        container.Register(dependency, out var registration);

        // Assert
        Assert.That(container.CallResults.Spec.Contract, Is.EqualTo(typeof(Mock.IContract)));
        Assert.That(container.CallResults.Spec.Lifetime, Is.EqualTo(DependencyLifetime.Singleton));
        Assert.That(container.CallResults.Spec.ImplementationFactory(container),
            Is.EqualTo(dependency));

        Assert.That(registration, Is.EqualTo(container.CallResults.Registration));
    }

    [Test]
    public void Register_ForDependencyInstanceWithoutOut_CallsWithSpecAndIgnoresOut()
    {
        // Set up
        var lifetime = Mock.RandomLifetime;
        var container = new Mock.DependencyContainer();
        var dependency = Substitute.For<Mock.IContract>();

        // Act
        container.Register(dependency);

        // Assert
        Assert.That(container.CallResults.Spec.Contract, Is.EqualTo(typeof(Mock.IContract)));
        Assert.That(container.CallResults.Spec.Lifetime, Is.EqualTo(DependencyLifetime.Singleton));
        Assert.That(container.CallResults.Spec.ImplementationFactory(container),
            Is.EqualTo(dependency));
    }

    [Test]
    public void Register_SpecificationOnly_CallsAndIgnoresOut()
    {
        // Set up
        var container = Substitute.For<IDependencyContainer>();
        var spec = new DependencySpecification()
        {
            Contract = typeof(Mock.IContract),
            ImplementationFactory = _ => Substitute.For<Mock.IContract>()
        };

        // Act
        container.Register(spec);

        // Assert
        container.Received().Register(spec, out _);
    }
}
