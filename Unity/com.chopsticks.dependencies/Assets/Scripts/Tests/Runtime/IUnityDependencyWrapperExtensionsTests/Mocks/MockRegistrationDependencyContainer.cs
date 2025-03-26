using Chopsticks.Dependencies.Containers;
using Chopsticks.Dependencies.Resolutions;
using MonoContainerTests.Mocks;
using System;
using System.Collections.Generic;

namespace IUnityDependencyWrapperExtensionsTests.Mocks
{
    public class MockRegistrationDependencyContainer : MockDependencyContainer
    {
        public bool RegistrationWasCalled { get; private set; }
        public DependencySpecification RegistrationSpecification { get; private set; }
        public DependencyRegistration ProvidedRegistration { get; private set; }

        public override bool InheritParentDependencies 
        { 
            get => throw new NotImplementedException(); 
            set => throw new NotImplementedException(); 
        }
        public override IDependencyResolutionProvider Parent 
        { 
            get => throw new NotImplementedException(); 
            set => throw new NotImplementedException(); 
        }

        public override bool CanProvide(Type contract)
        {
            throw new NotImplementedException();
        }

        public override IDependencyContainer Deregister(DependencyRegistration registration)
        {
            throw new NotImplementedException();
        }

        public override void Dispose()
        {
            throw new NotImplementedException();
        }

        public override DependencyResolution GetResolution(Type contract)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<DependencyResolution> GetResolutions()
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<DependencyResolution> GetResolutions(Type contract)
        {
            throw new NotImplementedException();
        }

        public override IDependencyContainer Register(DependencySpecification specification, 
            out DependencyRegistration registration)
        {
            RegistrationWasCalled = true;
            RegistrationSpecification = specification;
            ProvidedRegistration = new()
            { 
                Contract = specification.Contract 
            };

            registration = ProvidedRegistration;
            return this;
        }

        public override bool Resolve(Type contract, out object implementation)
        {
            throw new NotImplementedException();
        }

        public override IEnumerable<object> ResolveAll(Type contract)
        {
            throw new NotImplementedException();
        }
    }
}