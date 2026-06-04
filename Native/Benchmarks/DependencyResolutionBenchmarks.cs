using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using Chopsticks.Dependencies;
using Chopsticks.Dependencies.Containers;

namespace Chopsticks.Benchmarks;

/// <summary>
/// Benchmarks for dependency resolution.
/// Tests CHOP-003 (singleton thread safety), CHOP-004 (contained thread safety), CHOP-010 (double lookup).
/// </summary>
[MemoryDiagnoser]
[SimpleJob(warmupCount: 3, iterationCount: 10)]
public class DependencyResolutionBenchmarks
{
    private DependencyContainer _container = null!;
    private DependencyContainer _containerWithMultiple = null!;
    private DependencyRegistration _registration = null!;

    public interface ITestService { }
    public class TestService : ITestService { }
    public interface IOtherService { }
    public class OtherService : IOtherService { }

    [GlobalSetup]
    public void Setup()
    {
        _container = new DependencyContainer();
        _container.Register<ITestService>(c => new TestService());

        _containerWithMultiple = new DependencyContainer();
        _containerWithMultiple.Register<ITestService>(c => new TestService());
        _containerWithMultiple.Register<IOtherService>(c => new OtherService());
        _containerWithMultiple.Register(new DependencySpecification
        {
            Contract = typeof(string),
            ImplementationFactory = _ => "test",
            Lifetime = DependencyLifetime.Singleton
        }, out _registration);
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        _container.Dispose();
        _containerWithMultiple.Dispose();
    }

    /// <summary>
    /// Singleton resolution after initialization (fast path).
    /// Expected: Near-zero allocation, just returns cached instance.
    /// </summary>
    [Benchmark(Description = "Resolve_Singleton_Cached")]
    public ITestService? Resolve_Singleton_Cached()
    {
        _container.Resolve<ITestService>(out var service);
        return service;
    }

    /// <summary>
    /// Resolution throughput test (1000 resolutions).
    /// Amplifies any per-resolution overhead.
    /// </summary>
    [Benchmark(Description = "Resolve_1000x_Throughput")]
    public int Resolve_Throughput()
    {
        int count = 0;
        for (int i = 0; i < 1000; i++)
        {
            if (_container.Resolve<ITestService>(out _))
                count++;
        }
        return count;
    }

    /// <summary>
    /// Deregister operation (tests CHOP-010 double lookup fix).
    /// Note: This modifies state, so it's a synthetic benchmark.
    /// </summary>
    [Benchmark(Description = "Deregister_SingleLookup")]
    public IDependencyContainer Deregister_And_ReRegister()
    {
        _containerWithMultiple.Deregister(_registration);
        _containerWithMultiple.Register(new DependencySpecification
        {
            Contract = typeof(string),
            ImplementationFactory = _ => "test",
            Lifetime = DependencyLifetime.Singleton
        }, out _registration);
        return _containerWithMultiple;
    }

    /// <summary>
    /// CanProvide check (dictionary lookup).
    /// </summary>
    [Benchmark(Description = "CanProvide_Check")]
    public bool CanProvide_Check()
    {
        return _container.CanProvide(typeof(ITestService));
    }
}
