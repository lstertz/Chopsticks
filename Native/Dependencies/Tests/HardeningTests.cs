using Chopsticks.Dependencies;
using Chopsticks.Dependencies.Containers;
using System.Collections.Concurrent;

namespace DependencyHardeningTests;

/// <summary>
/// Aggressive concurrency and correctness tests for the dependency container,
/// focused on the supported concurrent scenario: many threads resolving against
/// a container whose registrations are fixed (registration is a startup concern).
/// </summary>
[TestFixture]
public class HardeningTests
{
    public interface IService { int Id { get; } }

    public sealed class Service : IService
    {
        private static int _next;
        public int Id { get; } = Interlocked.Increment(ref _next);
    }

    private const int Threads = 32;
    private const int PerThread = 1000;

    private static void RunConcurrently(Action<int> body)
    {
        var errors = new ConcurrentBag<Exception>();
        var tasks = Enumerable.Range(0, Threads).Select(t => Task.Run(() =>
        {
            try { for (int i = 0; i < PerThread; i++) body(t); }
            catch (Exception ex) { errors.Add(ex); }
        })).ToArray();
        Task.WaitAll(tasks);
        Assert.That(errors, Is.Empty, "no exceptions during concurrent resolution");
    }

    [Test]
    [Description("A singleton must resolve to the exact same instance across all threads; factory runs once.")]
    public void Singleton_ConcurrentResolve_SingleInstance()
    {
        int factoryCalls = 0;
        var container = new DependencyContainer();
        container.Register<IService>(_ =>
        {
            Interlocked.Increment(ref factoryCalls);
            return new Service();
        }, DependencyLifetime.Singleton);

        var seen = new ConcurrentBag<int>();
        RunConcurrently(_ =>
        {
            container.Resolve<IService>(out var svc);
            seen.Add(svc!.Id);
        });

        Assert.That(factoryCalls, Is.EqualTo(1), "singleton factory must run exactly once");
        Assert.That(seen.Distinct().Count(), Is.EqualTo(1), "all threads observe the same instance");
    }

    [Test]
    [Description("A contained dependency is one-instance-per-container, isolated across containers.")]
    public void Contained_PerContainer_IsolatedAndStable()
    {
        var root = new DependencyContainer();
        root.Register<IService>(_ => new Service(), DependencyLifetime.Contained);

        var childA = new DependencyContainer { Parent = root };
        var childB = new DependencyContainer { Parent = root };

        var aIds = new ConcurrentBag<int>();
        var bIds = new ConcurrentBag<int>();
        var rootIds = new ConcurrentBag<int>();

        RunConcurrently(_ =>
        {
            root.Resolve<IService>(out var r);
            childA.Resolve<IService>(out var a);
            childB.Resolve<IService>(out var b);
            rootIds.Add(r!.Id);
            aIds.Add(a!.Id);
            bIds.Add(b!.Id);
        });

        Assert.That(rootIds.Distinct().Count(), Is.EqualTo(1), "root sees one contained instance");
        Assert.That(aIds.Distinct().Count(), Is.EqualTo(1), "childA sees one contained instance");
        Assert.That(bIds.Distinct().Count(), Is.EqualTo(1), "childB sees one contained instance");
        Assert.That(new[] { rootIds.First(), aIds.First(), bIds.First() }.Distinct().Count(),
            Is.EqualTo(3), "each container has its own contained instance");
    }

    [Test]
    [Description("Transient resolution yields a fresh instance every call, even under contention.")]
    public void Transient_ConcurrentResolve_AllDistinct()
    {
        int factoryCalls = 0;
        var container = new DependencyContainer();
        container.Register<IService>(_ =>
        {
            Interlocked.Increment(ref factoryCalls);
            return new Service();
        }, DependencyLifetime.Transient);

        var ids = new ConcurrentBag<int>();
        RunConcurrently(_ =>
        {
            container.Resolve<IService>(out var svc);
            ids.Add(svc!.Id);
        });

        Assert.That(factoryCalls, Is.EqualTo(Threads * PerThread));
        Assert.That(ids.Count, Is.EqualTo(Threads * PerThread));
        Assert.That(ids.Distinct().Count(), Is.EqualTo(Threads * PerThread), "all transient instances distinct");
    }

    [Test]
    [Description("Deep parent chains resolve inherited singletons consistently under load.")]
    public void DeepParentChain_InheritedResolve_Consistent()
    {
        var root = new DependencyContainer();
        var expected = new Service();
        root.Register<IService>(_ => expected, DependencyLifetime.Singleton);

        // Build a 20-deep chain of inheriting child containers.
        var current = root;
        for (int i = 0; i < 20; i++)
            current = new DependencyContainer { Parent = current };

        var leaf = current;
        var ids = new ConcurrentBag<int>();
        RunConcurrently(_ =>
        {
            leaf.Resolve<IService>(out var svc);
            ids.Add(svc!.Id);
        });

        Assert.That(ids.Distinct().Count(), Is.EqualTo(1));
        Assert.That(ids.First(), Is.EqualTo(expected.Id));
    }

    [Test]
    [Description("ResolveAll returns every registered implementation, concurrently consistent.")]
    public void ResolveAll_MultipleRegistrations_AllReturned()
    {
        var container = new DependencyContainer();
        container.Register<IService>(_ => new Service(), DependencyLifetime.Singleton);
        container.Register<IService>(_ => new Service(), DependencyLifetime.Singleton);
        container.Register<IService>(_ => new Service(), DependencyLifetime.Singleton);

        var counts = new ConcurrentBag<int>();
        RunConcurrently(_ =>
        {
            int count = container.ResolveAll<IService>().Count();
            counts.Add(count);
        });

        Assert.That(counts.Distinct().Single(), Is.EqualTo(3),
            "every concurrent ResolveAll observes all three registrations");
    }

    [Test]
    [Description("Child does not leak its own registration to the parent or siblings.")]
    public void ChildRegistration_DoesNotEscapeToParent()
    {
        var root = new DependencyContainer();
        root.Register<IService>(_ => new Service(), DependencyLifetime.Singleton);

        var child = new DependencyContainer { Parent = root };
        child.Register<IService>(_ => new Service(), DependencyLifetime.Singleton);

        root.Resolve<IService>(out var rootSvc);
        child.Resolve<IService>(out var childSvc);

        // Child resolves its own first registration, root resolves its own.
        Assert.That(childSvc!.Id, Is.Not.EqualTo(rootSvc!.Id));
        // Parent must only see its single registration.
        Assert.That(root.ResolveAll<IService>().Count(), Is.EqualTo(1));
    }
}
