using Chopsticks.Dependencies.Containers;
using Chopsticks.Dependencies.Resolutions;
using System;
using System.Collections.Generic;

namespace MonoContainerTests.Mocks
{
    public abstract class MockDependencyContainer :
        IDependencyContainer, IDependencyResolutionProvider, IDisposable
    {
        public class Definition { }

        public abstract bool InheritParentDependencies { get; set; }
        public abstract IDependencyResolutionProvider Parent { get; set; }

        public abstract bool CanProvide(Type contract);
        public abstract IDependencyContainer Deregister(DependencyRegistration registration);
        public abstract void Dispose();
        public abstract DependencyResolution GetResolution(Type contract);
        public abstract IEnumerable<DependencyResolution> GetResolutions(Type contract);
        public abstract IEnumerable<DependencyResolution> GetResolutionsForDisposal();
        public abstract IDependencyContainer Register(DependencySpecification specification,
            out DependencyRegistration registration);
        public abstract bool Resolve(Type contract, out object implementation);
        public abstract IEnumerable<object> ResolveAll(Type contract);
    }
}