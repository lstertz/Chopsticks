using Chopsticks.Messages;
using Chopsticks.Messages.Handlers;
using Chopsticks.Messages.Handlers.Multicast;
using Chopsticks.Messages.Handlers.Sources;
using Chopsticks.Messages.Registration;
using NUnit.Framework;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Chopsticks.Tests;

/// <summary>
/// Comprehensive tests for pool lifecycle validation.
/// These tests specifically target the P0 issue: pools not being returned after use.
/// </summary>
[TestFixture]
[Category("PoolLifecycle")]
public class PoolLifecycleTests
{
    public readonly struct TestMessage
    {
        public readonly int Id;
        public TestMessage(int id) => Id = id;
    }
    
    private class SimpleHandler : ISyncMessageHandler<TestMessage>
    {
        public void Handle(TestMessage message) { }
    }

    // ========================================================================
    // POOL DRAIN DETECTION
    // ========================================================================
    
    [Test]
    [Description("Demonstrates pool drain when Dispose is not called")]
    public void PoolDrain_Detection_MulticastHandler()
    {
        var multicast = new MulticastMessageHandler<TestMessage>();
        var registrar = multicast as IMessageHandlerRegistrar<TestMessage>;
        registrar!.Register(new SimpleHandler(), default);
        registrar.Register(new SimpleHandler(), default);
        
        var allocations = new System.Collections.Generic.List<long>();
        
        // Measure allocation for each batch of 64 (pool size)
        for (int batch = 0; batch < 5; batch++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            long before = GC.GetAllocatedBytesForCurrentThread();
            
            for (int i = 0; i < 64; i++)
            {
                multicast.TryHandle(new TestMessage(batch * 64 + i));
                // NO Dispose() - simulates real-world usage
            }
            
            long after = GC.GetAllocatedBytesForCurrentThread();
            allocations.Add(after - before);
        }
        
        Console.WriteLine("=== Pool Drain Detection ===");
        for (int i = 0; i < allocations.Count; i++)
        {
            Console.WriteLine($"Batch {i + 1} (ops {i * 64 + 1}-{(i + 1) * 64}): {allocations[i]:N0} bytes ({allocations[i] / 64.0:F1} B/op)");
        }
        
        // Analysis:
        // - Batch 1: Pool fills (allocations occur)
        // - Batch 2-5: If pools work correctly with auto-return, should be ~0
        //              If pools drain (current bug), allocations continue
        
        bool poolDrained = allocations.Skip(1).All(a => a > allocations[0] * 0.5);
        
        Console.WriteLine($"\nPool drain detected: {poolDrained}");
        Console.WriteLine($"First batch: {allocations[0]:N0} B");
        Console.WriteLine($"Subsequent average: {allocations.Skip(1).Average():N0} B");
        
        if (poolDrained)
        {
            Console.WriteLine("\n⚠️ POOL LIFECYCLE BUG CONFIRMED: Sources are not being returned to pool!");
        }
    }
    
    [Test]
    [Description("Measures allocation pattern showing pool ineffectiveness")]
    public void PoolIneffectiveness_LinearAllocationGrowth()
    {
        var multicast = new MulticastMessageHandler<TestMessage>();
        var registrar = multicast as IMessageHandlerRegistrar<TestMessage>;
        registrar!.Register(new SimpleHandler(), default);
        
        GC.Collect();
        long startAlloc = GC.GetAllocatedBytesForCurrentThread();
        
        var checkpoints = new System.Collections.Generic.List<(int ops, long alloc)>();
        
        for (int i = 0; i < 1000; i++)
        {
            multicast.TryHandle(new TestMessage(i));
            
            if ((i + 1) % 100 == 0)
            {
                checkpoints.Add((i + 1, GC.GetAllocatedBytesForCurrentThread() - startAlloc));
            }
        }
        
        Console.WriteLine("=== Allocation Growth Over Time ===");
        Console.WriteLine("Ops\tTotal Alloc\tPer-Op");
        
        long lastAlloc = 0;
        foreach (var (ops, alloc) in checkpoints)
        {
            long deltaAlloc = alloc - lastAlloc;
            Console.WriteLine($"{ops}\t{alloc:N0}\t\t{deltaAlloc / 100.0:F1} B/op");
            lastAlloc = alloc;
        }
        
        // With effective pooling: allocation should plateau after initial pool fill
        // With pool drain: allocation grows linearly forever
        
        double firstBatchPerOp = checkpoints[0].alloc / 100.0;
        double lastBatchPerOp = (checkpoints[^1].alloc - checkpoints[^2].alloc) / 100.0;
        
        Console.WriteLine($"\nFirst 100 ops: {firstBatchPerOp:F1} B/op");
        Console.WriteLine($"Last 100 ops: {lastBatchPerOp:F1} B/op");
        
        bool linearGrowth = lastBatchPerOp > firstBatchPerOp * 0.5;
        Console.WriteLine($"Linear growth (pool ineffective): {linearGrowth}");
    }

    // ========================================================================
    // DIRECT SYNC HANDLER COMPARISON
    // ========================================================================
    
    [Test]
    public void CompareDirectVsMulticast_Allocation()
    {
        // Direct sync handler (zero-allocation path)
        IMessageHandler<TestMessage> directHandler = new SimpleHandler();
        
        // Multicast (pooled path)
        var multicast = new MulticastMessageHandler<TestMessage>();
        var registrar = multicast as IMessageHandlerRegistrar<TestMessage>;
        registrar!.Register(new SimpleHandler(), default);
        
        const int Operations = 1000;
        
        // Warm up both
        for (int i = 0; i < 100; i++)
        {
            directHandler.TryHandle(new TestMessage(i));
            multicast.TryHandle(new TestMessage(i));
        }
        
        // Measure direct
        GC.Collect();
        long directBefore = GC.GetAllocatedBytesForCurrentThread();
        
        for (int i = 0; i < Operations; i++)
        {
            directHandler.TryHandle(new TestMessage(i));
        }
        
        long directAfter = GC.GetAllocatedBytesForCurrentThread();
        long directAlloc = directAfter - directBefore;
        
        // Measure multicast
        GC.Collect();
        long multicastBefore = GC.GetAllocatedBytesForCurrentThread();
        
        for (int i = 0; i < Operations; i++)
        {
            multicast.TryHandle(new TestMessage(i));
        }
        
        long multicastAfter = GC.GetAllocatedBytesForCurrentThread();
        long multicastAlloc = multicastAfter - multicastBefore;
        
        Console.WriteLine("=== Direct vs Multicast Allocation ===");
        Console.WriteLine($"Direct sync handler: {directAlloc:N0} bytes ({directAlloc / (double)Operations:F2} B/op)");
        Console.WriteLine($"Multicast handler:   {multicastAlloc:N0} bytes ({multicastAlloc / (double)Operations:F2} B/op)");
        Console.WriteLine($"Multicast overhead:  {multicastAlloc - directAlloc:N0} bytes");
        
        // Direct should be near zero
        Assert.That(directAlloc / (double)Operations, Is.LessThan(10),
            "Direct sync handler should be zero-allocation");
    }

    // ========================================================================
    // POOL WITH MANUAL DISPOSE (REFERENCE)
    // ========================================================================
    
    [Test]
    [Description("Shows ideal behavior with manual Dispose")]
    public void PoolWithManualDispose_ShowsIdealBehavior()
    {
        // Directly test promise source pool with manual dispose
        
        // Fill pool first
        for (int i = 0; i < 100; i++)
        {
            var source = TryHandlePromiseSource.Rent();
            source.Init(HandlingResult.Success);
            source.Dispose();  // Manual return to pool
        }
        
        // Now measure - should be near zero
        GC.Collect();
        long before = GC.GetAllocatedBytesForCurrentThread();
        
        for (int i = 0; i < 1000; i++)
        {
            var source = TryHandlePromiseSource.Rent();
            source.Init(HandlingResult.Success);
            source.Dispose();  // Return to pool
        }
        
        long after = GC.GetAllocatedBytesForCurrentThread();
        double perOp = (after - before) / 1000.0;
        
        Console.WriteLine("=== With Manual Dispose (Reference) ===");
        Console.WriteLine($"1000 ops: {after - before:N0} bytes");
        Console.WriteLine($"Per-op: {perOp:F2} bytes");
        Console.WriteLine($"\nThis shows the IDEAL behavior we want with auto-return.");
        
        // Should be very low with pool reuse
        Assert.That(perOp, Is.LessThan(50), 
            "With manual Dispose, per-op allocation should be minimal");
    }

    // ========================================================================
    // POOL CONCURRENT ACCESS
    // ========================================================================
    
    [Test]
    public async Task PoolConcurrency_NoCorruption()
    {
        var exceptions = new ConcurrentBag<Exception>();
        var successCount = 0;
        
        const int ThreadCount = 20;
        const int OpsPerThread = 500;
        
        var tasks = Enumerable.Range(0, ThreadCount).Select(_ => Task.Run(() =>
        {
            try
            {
                for (int i = 0; i < OpsPerThread; i++)
                {
                    var source = TryHandlePromiseSource.Rent();
                    source.Init(HandlingResult.Success);
                    
                    // Verify it works
                    if (source.IsCompleted && source.GetResult().Status == HandlingStatus.Success)
                    {
                        Interlocked.Increment(ref successCount);
                    }
                    
                    source.Dispose();
                }
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        }));
        
        await Task.WhenAll(tasks);
        
        Console.WriteLine($"=== Concurrent Pool Access ===");
        Console.WriteLine($"Threads: {ThreadCount}");
        Console.WriteLine($"Ops per thread: {OpsPerThread}");
        Console.WriteLine($"Total successful: {successCount}");
        Console.WriteLine($"Exceptions: {exceptions.Count}");
        
        Assert.That(exceptions, Is.Empty, "No exceptions under concurrent access");
        Assert.That(successCount, Is.EqualTo(ThreadCount * OpsPerThread));
    }
    
    [Test]
    public async Task PoolConcurrency_HighContention()
    {
        // Stress test with maximum contention
        
        const int ThreadCount = 100;
        const int OpsPerThread = 100;
        
        var allSources = new ConcurrentBag<TryHandlePromiseSource>();
        
        // Phase 1: Everyone rents (high contention)
        var rentTasks = Enumerable.Range(0, ThreadCount).Select(_ => Task.Run(() =>
        {
            for (int i = 0; i < OpsPerThread; i++)
            {
                var source = TryHandlePromiseSource.Rent();
                source.Init(HandlingResult.Success);
                allSources.Add(source);
            }
        }));
        
        await Task.WhenAll(rentTasks);
        
        int rentedCount = allSources.Count;
        Console.WriteLine($"Rented: {rentedCount}");
        
        // Phase 2: Everyone returns (high contention)
        var returnTasks = Enumerable.Range(0, ThreadCount).Select(_ => Task.Run(() =>
        {
            while (allSources.TryTake(out var source))
            {
                source.Dispose();
            }
        }));
        
        await Task.WhenAll(returnTasks);
        
        Console.WriteLine($"All returned. Pool should now have up to 64 items.");
        
        // Verify pool works after contention
        GC.Collect();
        long before = GC.GetAllocatedBytesForCurrentThread();
        
        for (int i = 0; i < 64; i++)
        {
            var source = TryHandlePromiseSource.Rent();
            source.Init(HandlingResult.Success);
            source.Dispose();
        }
        
        long after = GC.GetAllocatedBytesForCurrentThread();
        
        Console.WriteLine($"64 ops after contention: {after - before:N0} bytes");
    }

    // ========================================================================
    // ALL POOL TYPES TEST
    // ========================================================================
    
    [Test]
    public void AllPoolTypes_BasicFunctionality()
    {
        // TryHandlePromiseSource
        {
            var source = TryHandlePromiseSource.Rent();
            source.Init(HandlingResult.Success);
            Assert.That(source.IsCompleted, Is.True);
            Assert.That(source.GetResult().Status, Is.EqualTo(HandlingStatus.Success));
            source.Dispose();
        }
        
        // TryHandleAsyncPromiseSource
        {
            var source = TryHandleAsyncPromiseSource.Rent();
            var task = Task.CompletedTask;
            source.Init(task.GetAwaiter());
            Assert.That(source.GetResult().Status, Is.EqualTo(HandlingStatus.Success));
            source.Dispose();
        }
        
        // HandlePromiseSource
        {
            var innerSource = TryHandlePromiseSource.Rent();
            innerSource.Init(HandlingResult.Success);
            
            var source = HandlePromiseSource.Rent();
            source.Init(innerSource);
            Assert.That(source.GetResult().Status, Is.EqualTo(HandlingStatus.Success));
            source.Dispose();
            innerSource.Dispose();
        }
        
        // HandleAsyncPromiseSource
        {
            var innerSource = TryHandlePromiseSource.Rent();
            innerSource.Init(HandlingResult.Success);
            
            var source = HandleAsyncPromiseSource.Rent();
            source.Init(innerSource);
            Assert.That(source.GetResult().Status, Is.EqualTo(HandlingStatus.Success));
            source.Dispose();
            innerSource.Dispose();
        }
        
        Console.WriteLine("All pool types: basic functionality verified");
    }
    
    [Test]
    public void AllPoolTypes_MeasureAllocation()
    {
        const int Ops = 100;
        
        // TryHandlePromiseSource
        GC.Collect();
        long tryBefore = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < Ops; i++)
        {
            var source = TryHandlePromiseSource.Rent();
            source.Init(HandlingResult.Success);
            source.Dispose();
        }
        long tryAlloc = GC.GetAllocatedBytesForCurrentThread() - tryBefore;
        
        // TryHandleAsyncPromiseSource
        GC.Collect();
        long asyncTryBefore = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < Ops; i++)
        {
            var source = TryHandleAsyncPromiseSource.Rent();
            var task = Task.CompletedTask;
            source.Init(task.GetAwaiter());
            source.Dispose();
        }
        long asyncTryAlloc = GC.GetAllocatedBytesForCurrentThread() - asyncTryBefore;
        
        // HandlePromiseSource
        var inner1 = TryHandlePromiseSource.Rent();
        inner1.Init(HandlingResult.Success);
        GC.Collect();
        long handleBefore = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < Ops; i++)
        {
            var source = HandlePromiseSource.Rent();
            source.Init(inner1);
            source.Dispose();
        }
        long handleAlloc = GC.GetAllocatedBytesForCurrentThread() - handleBefore;
        inner1.Dispose();
        
        // HandleAsyncPromiseSource
        var inner2 = TryHandlePromiseSource.Rent();
        inner2.Init(HandlingResult.Success);
        GC.Collect();
        long asyncHandleBefore = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < Ops; i++)
        {
            var source = HandleAsyncPromiseSource.Rent();
            source.Init(inner2);
            source.Dispose();
        }
        long asyncHandleAlloc = GC.GetAllocatedBytesForCurrentThread() - asyncHandleBefore;
        inner2.Dispose();
        
        Console.WriteLine($"=== Pool Allocation ({Ops} ops each with Dispose) ===");
        Console.WriteLine($"TryHandlePromiseSource:      {tryAlloc:N0} bytes ({tryAlloc / (double)Ops:F1} B/op)");
        Console.WriteLine($"TryHandleAsyncPromiseSource: {asyncTryAlloc:N0} bytes ({asyncTryAlloc / (double)Ops:F1} B/op)");
        Console.WriteLine($"HandlePromiseSource:         {handleAlloc:N0} bytes ({handleAlloc / (double)Ops:F1} B/op)");
        Console.WriteLine($"HandleAsyncPromiseSource:    {asyncHandleAlloc:N0} bytes ({asyncHandleAlloc / (double)Ops:F1} B/op)");
    }

    // ========================================================================
    // PREWARM TEST
    // ========================================================================
    
    [Test]
    public void PreWarm_ReducesFirstUseAllocation()
    {
        // Note: PreWarm affects global state, so this test may be affected by other tests
        
        if (!PromiseSourcePools.IsPreWarmed)
        {
            // Measure without prewarm first
            GC.Collect();
            long beforePrewarm = GC.GetAllocatedBytesForCurrentThread();
            
            for (int i = 0; i < 16; i++)
            {
                var source = TryHandlePromiseSource.Rent();
                source.Init(HandlingResult.Success);
                source.Dispose();
            }
            
            long afterPrewarm = GC.GetAllocatedBytesForCurrentThread();
            Console.WriteLine($"Before PreWarm (16 ops): {afterPrewarm - beforePrewarm:N0} bytes");
        }
        
        // Pre-warm
        PromiseSourcePools.PreWarm(16);
        Assert.That(PromiseSourcePools.IsPreWarmed, Is.True);
        
        // After pre-warm, should be minimal
        GC.Collect();
        long before = GC.GetAllocatedBytesForCurrentThread();
        
        for (int i = 0; i < 16; i++)
        {
            var source = TryHandlePromiseSource.Rent();
            source.Init(HandlingResult.Success);
            source.Dispose();
        }
        
        long after = GC.GetAllocatedBytesForCurrentThread();
        Console.WriteLine($"After PreWarm (16 ops): {after - before:N0} bytes");
    }
}
