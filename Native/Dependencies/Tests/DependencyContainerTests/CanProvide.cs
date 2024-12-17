﻿using Chopsticks.Dependencies.Containers;
using Chopsticks.Dependencies.Resolutions;

namespace DependencyContainerTests;

public class CanProvide
{
    private static class Mock
    {
        public interface IContractA { }

        public interface IContractB { }

        public class ImplementationA : IContractA { }
    }


    [Test]
    public void Contains_DeregisteredContractedImplementation_FalseForContract()
    {
        // Set up
        DependencyContainer container = new();
        DependencySpecification spec = new()
        {
            Contract = typeof(Mock.IContractA),
            Implementation = typeof(Mock.ImplementationA),
            ImplementationFactory = _ => new()
        };

        // Act
        container.Register(spec, out var registration)
            .Deregister(registration);
        var contains = (container as IDependencyResolutionProvider).CanProvide(spec.Contract);

        // Assert
        Assert.That(contains, Is.False);
    }

    [Test]
    public void Contains_DeregisteredFirstAfterMultipleRegistrations_True()
    {
        // Set up
        DependencyContainer container = new();
        DependencySpecification spec1 = new()
        {
            Contract = typeof(Mock.IContractA),
            Implementation = typeof(Mock.ImplementationA),
            ImplementationFactory = _ => new()
        };
        DependencySpecification spec2 = new()
        {
            Contract = typeof(Mock.IContractA),
            Implementation = typeof(Mock.ImplementationA),
            ImplementationFactory = _ => new()
        };

        // Act
        container.Register(spec1, out var registration1)
            .Register(spec2, out _)
            .Deregister(registration1);
        var contains = (container as IDependencyResolutionProvider).CanProvide(spec2.Contract);

        // Assert
        Assert.That(contains, Is.True);
    }

    [Test]
    public void Contains_DeregisteredImplementationForSelf_FalseForImplementation()
    {
        // Set up
        DependencyContainer container = new();
        DependencySpecification spec = new()
        {
            Contract = typeof(Mock.ImplementationA),
            Implementation = typeof(Mock.ImplementationA),
            ImplementationFactory = _ => new()
        };

        // Act
        container.Register(spec, out var registration)
            .Deregister(registration);
        var contains = (container as IDependencyResolutionProvider).CanProvide(
            spec.Implementation);

        // Assert
        Assert.That(contains, Is.False);
    }

    [Test]
    public void Contains_DeregisteredMultipleAfterMultipleRegistrations_True()
    {
        // Set up
        DependencyContainer container = new();
        DependencySpecification spec1 = new()
        {
            Contract = typeof(Mock.IContractA),
            Implementation = typeof(Mock.ImplementationA),
            ImplementationFactory = _ => new()
        };
        DependencySpecification spec2 = new()
        {
            Contract = typeof(Mock.IContractA),
            Implementation = typeof(Mock.ImplementationA),
            ImplementationFactory = _ => new()
        };

        // Act
        container.Register(spec1, out var registration1)
            .Register(spec2, out var registration2)
            .Deregister(registration1)
            .Deregister(registration2);
        var contains = (container as IDependencyResolutionProvider).CanProvide(spec2.Contract);

        // Assert
        Assert.That(contains, Is.False);
    }

    [Test]
    public void Contains_DeregisteredSecondAfterMultipleRegistrations_True()
    {
        // Set up
        DependencyContainer container = new();
        DependencySpecification spec1 = new()
        {
            Contract = typeof(Mock.IContractA),
            Implementation = typeof(Mock.ImplementationA),
            ImplementationFactory = _ => new()
        };
        DependencySpecification spec2 = new()
        {
            Contract = typeof(Mock.IContractA),
            Implementation = typeof(Mock.ImplementationA),
            ImplementationFactory = _ => new()
        };

        // Act
        container.Register(spec1, out _)
            .Register(spec2, out var registration2)
            .Deregister(registration2);
        var contains = (container as IDependencyResolutionProvider).CanProvide(spec1.Contract);

        // Assert
        Assert.That(contains, Is.True);
    }


    [Test]
    public void Contains_InheritanceEnabledAndAvailable_True()
    {
        // Set up
        DependencyContainer parentContainer = new();
        DependencyContainer childContainer = new()
        {
            InheritParentDependencies = true,
            Parent = parentContainer
        };

        DependencySpecification spec = new()
        {
            Contract = typeof(Mock.IContractA),
            Implementation = typeof(Mock.ImplementationA),
            ImplementationFactory = _ => new()
        };

        // Act
        parentContainer.Register(spec, out _);
        var contains = (childContainer as IDependencyResolutionProvider).CanProvide(spec.Contract);

        // Assert
        Assert.That(contains, Is.True);
    }

    [Test]
    public void Contains_InheritanceEnabledAndUnavailable_False()
    {
        // Set up
        DependencyContainer parentContainer = new();
        DependencyContainer childContainer = new()
        {
            InheritParentDependencies = true,
            Parent = parentContainer
        };

        DependencySpecification spec = new()
        {
            Contract = typeof(Mock.IContractA),
            Implementation = typeof(Mock.ImplementationA),
            ImplementationFactory = _ => new()
        };

        // Act
        parentContainer.Register(spec, out _);
        var contains = (childContainer as IDependencyResolutionProvider).CanProvide(
            typeof(Mock.IContractB));

        // Assert
        Assert.That(contains, Is.False);
    }

    [Test]
    public void Contains_InheritanceDisabledAndAvailable_False()
    {
        // Set up
        DependencyContainer parentContainer = new();
        DependencyContainer childContainer = new()
        {
            InheritParentDependencies = false,
            Parent = parentContainer
        };

        DependencySpecification spec = new()
        {
            Contract = typeof(Mock.IContractA),
            Implementation = typeof(Mock.ImplementationA),
            ImplementationFactory = _ => new()
        };

        // Act
        parentContainer.Register(spec, out _);
        var contains = (childContainer as IDependencyResolutionProvider).CanProvide(spec.Contract);

        // Assert
        Assert.That(contains, Is.False);
    }


    [Test]
    public void Contains_RegisteredContractedImplementation_FalseForImplementation()
    {
        // Set up
        DependencyContainer container = new();
        DependencySpecification spec = new()
        {
            Contract = typeof(Mock.IContractA),
            Implementation = typeof(Mock.ImplementationA),
            ImplementationFactory = _ => new()
        };

        // Act
        container.Register(spec, out _);
        var contains = (container as IDependencyResolutionProvider).CanProvide(
            spec.Implementation);

        // Assert
        Assert.That(contains, Is.False);
    }

    [Test]
    public void Contains_RegisteredContractedImplementation_FalseForUnregisteredContract()
    {
        // Set up
        DependencyContainer container = new();
        DependencySpecification spec = new()
        {
            Contract = typeof(Mock.IContractA),
            Implementation = typeof(Mock.ImplementationA),
            ImplementationFactory = _ => new()
        };

        // Act
        container.Register(spec, out _);
        var contains = (container as IDependencyResolutionProvider).CanProvide(
            typeof(Mock.IContractB));

        // Assert
        Assert.That(contains, Is.False);
    }

    [Test]
    public void Contains_RegisteredContractedImplementation_TrueForContract()
    {
        // Set up
        DependencyContainer container = new();
        DependencySpecification spec = new()
        {
            Contract = typeof(Mock.IContractA),
            Implementation = typeof(Mock.ImplementationA),
            ImplementationFactory = _ => new()
        };

        // Act
        container.Register(spec, out _);
        var contains = (container as IDependencyResolutionProvider).CanProvide(spec.Contract);

        // Assert
        Assert.That(contains, Is.True);
    }

    [Test]
    public void Contains_RegisteredImplementationForSelf_FalseForContract()
    {
        // Set up
        DependencyContainer container = new();
        DependencySpecification spec = new()
        {
            Contract = typeof(Mock.ImplementationA),
            Implementation = typeof(Mock.ImplementationA),
            ImplementationFactory = _ => new()
        };

        // Act
        container.Register(spec, out _);
        var contains = (container as IDependencyResolutionProvider).CanProvide(
            typeof(Mock.IContractA));

        // Assert
        Assert.That(contains, Is.False);
    }

    [Test]
    public void Contains_RegisteredImplementationForSelf_TrueForImplementation()
    {
        // Set up
        DependencyContainer container = new();
        DependencySpecification spec = new()
        {
            Contract = typeof(Mock.ImplementationA),
            Implementation = typeof(Mock.ImplementationA),
            ImplementationFactory = _ => new()
        };

        // Act
        container.Register(spec, out _);
        var contains = (container as IDependencyResolutionProvider).CanProvide(
            spec.Implementation);

        // Assert
        Assert.That(contains, Is.True);
    }
}