using Chopsticks.Dependencies.Containers;
using Chopsticks.Dependencies.Factories;
using Chopsticks.Dependencies.Resolutions;
using NSubstitute;

namespace DependencyContainerTests;

public class Dispose
{
    public static class Mock
    {
        public interface IContractA { }
    }

    public static class SetUp
    {
        public static DependencyContainer ChildContainer(
            out DependencyResolution parentResolution,
            bool makeParentAsGrandparent = false)
        {
            var spec = new DependencySpecification()
            {
                Contract = typeof(Mock.IContractA),
                ImplementationFactory = _ => Substitute.For<Mock.IContractA>()
            };

            var parentFactory = Substitute.For<IDependencyResolutionFactory>();
            var parentContainer = new DependencyContainer(parentFactory);
            parentResolution = ConfigureFactoryForSpec(parentFactory, parentContainer, spec);
            parentContainer.Register(spec, out _);

            DependencyContainer? intermediaryParent = makeParentAsGrandparent ?
                new(Substitute.For<IDependencyResolutionFactory>())
                {
                    InheritParentDependencies = true,
                    Parent = parentContainer
                } : null;

            var childFactory = Substitute.For<IDependencyResolutionFactory>();
            var childContainer = new DependencyContainer(childFactory)
            {
                InheritParentDependencies = true,
                Parent = intermediaryParent ?? parentContainer
            };
            parentResolution.Get(childContainer)
                .Returns(spec.ImplementationFactory(childContainer));

            return childContainer;
        }

        public static DependencyContainer StandardContainer(
            out DependencyResolution firstResolution,
            out DependencyResolution secondResolution)
        {
            var firstSpec = new DependencySpecification()
            {
                Contract = typeof(Mock.IContractA),
                ImplementationFactory = _ => Substitute.For<Mock.IContractA>()
            };
            var secondSpec = new DependencySpecification()
            {
                Contract = typeof(Mock.IContractA),
                ImplementationFactory = _ => Substitute.For<Mock.IContractA>()
            };

            var factory = Substitute.For<IDependencyResolutionFactory>();
            var container = new DependencyContainer(factory);
            firstResolution = ConfigureFactoryForSpec(factory, container, firstSpec);
            secondResolution = ConfigureFactoryForSpec(factory, container, secondSpec);
            container
                .Register(firstSpec, out _)
                .Register(secondSpec, out _);

            return container;
        }


        private static DependencyResolution ConfigureFactoryForSpec(
            IDependencyResolutionFactory factory,
            IDependencyContainer container,
            DependencySpecification spec)
        {
            var mockDependency = Substitute.For<Mock.IContractA>();

            var resolution = Substitute.For<DependencyResolution>(
                spec.Contract,
                spec.ImplementationFactory);
            resolution.Get(container).Returns(mockDependency);

            factory.BuildResolutionFor(spec).Returns(_ => resolution);

            return resolution;
        }
    }


    [Test]
    public void Dispose_WithGrandparentResolutions_DisposesGrandparentResolutionForSelf()
    {
        // Set up
        var container = SetUp.ChildContainer(out var grandparentResolution, true);

        // Act
        container.Dispose();

        // Assert
        grandparentResolution.Received().DisposeFor(container);
    }

    [Test]
    public void Dispose_WithOwnResolutions_DisposesResolutions()
    {
        // Set up
        var container = SetUp.StandardContainer(
            out var firstResolution, out var secondResolution);

        // Act
        container.Dispose();

        // Assert
        firstResolution.Received().Dispose();
        secondResolution.Received().Dispose();
    }

    [Test]
    public void Dispose_WithParentResolutions_DisposesParentResolutionForSelf()
    {
        // Set up
        var container = SetUp.ChildContainer(out var parentResolution);

        // Act
        container.Dispose();

        // Assert
        parentResolution.Received().DisposeFor(container);
    }
}
