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
/// Real-world scenario tests that validate framework behavior under production-like conditions.
/// These tests intentionally DO NOT call Dispose() to mirror actual usage patterns.
/// </summary>
[TestFixture]
[Category("RealWorld")]
public class RealWorldScenarioTests
{
    public readonly struct GameMessage
    {
        public readonly int Id;
        public readonly float Value;
        
        public GameMessage(int id, float value)
        {
            Id = id;
            Value = value;
        }
    }
    
    public readonly struct LargePayloadMessage
    {
        public readonly float[] Positions;
        
        public LargePayloadMessage(float[] positions) => Positions = positions;
    }

    private class CountingSyncHandler : ISyncMessageHandler<GameMessage>
    {
        public int HandleCount;
        
        public void Handle(GameMessage message)
        {
            Interlocked.Increment(ref HandleCount);
        }
    }
    
    private class SlowSyncHandler : ISyncMessageHandler<GameMessage>
    {
        private readonly int _spinIterations;
        
        public SlowSyncHandler(int spinIterations = 100)
        {
            _spinIterations = spinIterations;
        }
        
        public void Handle(GameMessage message)
        {
            Thread.SpinWait(_spinIterations);
        }
    }
    
    private class LargePayloadHandler : ISyncMessageHandler<LargePayloadMessage>
    {
        public void Handle(LargePayloadMessage message)
        {
            float sum = 0;
            if (message.Positions != null)
            {
                foreach (var pos in message.Positions)
                    sum += pos;
            }
        }
    }

    // ========================================================================
    // SINGLE HANDLER TESTS (Zero Allocation Path)
    // ========================================================================
    
    [Test]
    [Category("ZeroAllocation")]
    public void SyncHandler_TryHandle_ZeroAllocation()
    {
        IMessageHandler<GameMessage> handler = new CountingSyncHandler();
        
        // Warm up
        for (int i = 0; i < 100; i++)
        {
            handler.TryHandle(new GameMessage(i, i));
        }
        
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        long allocBefore = GC.GetAllocatedBytesForCurrentThread();
        
        for (int i = 0; i < 10000; i++)
        {
            handler.TryHandle(new GameMessage(i, i));
        }
        
        long allocAfter = GC.GetAllocatedBytesForCurrentThread();
        long totalAlloc = allocAfter - allocBefore;
        
        Console.WriteLine($"=== Sync Handler TryHandle Zero-Allocation Test ===");
        Console.WriteLine($"10K operations: {totalAlloc:N0} bytes");
        Console.WriteLine($"Per-op: {totalAlloc / 10000.0:F2} bytes");
        
        // V2.1 should be near 0 B for sync handlers
        Assert.That(totalAlloc / 10000.0, Is.LessThan(10), 
            "Sync handler TryHandle should be near zero-allocation");
    }
    
    [Test]
    [Category("ZeroAllocation")]
    public void SyncHandler_Handle_ZeroAllocation()
    {
        IMessageHandler<GameMessage> handler = new CountingSyncHandler();
        
        // Warm up
        for (int i = 0; i < 100; i++)
        {
            handler.Handle(new GameMessage(i, i));
        }
        
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        long allocBefore = GC.GetAllocatedBytesForCurrentThread();
        
        for (int i = 0; i < 10000; i++)
        {
            handler.Handle(new GameMessage(i, i));
        }
        
        long allocAfter = GC.GetAllocatedBytesForCurrentThread();
        long totalAlloc = allocAfter - allocBefore;
        
        Console.WriteLine($"=== Sync Handler Handle Zero-Allocation Test ===");
        Console.WriteLine($"10K operations: {totalAlloc:N0} bytes");
        Console.WriteLine($"Per-op: {totalAlloc / 10000.0:F2} bytes");
        
        Assert.That(totalAlloc / 10000.0, Is.LessThan(10), 
            "Sync handler Handle should be near zero-allocation");
    }

    // ========================================================================
    // MULTICAST HANDLER TESTS (Pool Path)
    // ========================================================================
    
    [Test]
    [Category("Multicast")]
    public void Multicast_MeasuresAllocationPerOperation()
    {
        // Use the static multicast - register handlers first
        // Note: This uses global state, so be careful with test isolation
        
        var handler = new MulticastMessageHandler<GameMessage>();
        var registrar = handler as IMessageHandlerRegistrar<GameMessage>;
        registrar!.Register(new CountingSyncHandler(), default);
        registrar.Register(new CountingSyncHandler(), default);
        
        // Warm up
        for (int i = 0; i < 100; i++)
        {
            handler.TryHandle(new GameMessage(i, i));
        }
        
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        long allocBefore = GC.GetAllocatedBytesForCurrentThread();
        
        const int Operations = 1000;
        for (int i = 0; i < Operations; i++)
        {
            handler.TryHandle(new GameMessage(i, i));
            // NO Dispose() - mirrors production
        }
        
        long allocAfter = GC.GetAllocatedBytesForCurrentThread();
        long totalAlloc = allocAfter - allocBefore;
        
        Console.WriteLine($"=== Multicast Handler Allocation Test ===");
        Console.WriteLine($"{Operations} operations: {totalAlloc:N0} bytes");
        Console.WriteLine($"Per-op: {totalAlloc / (double)Operations:F1} bytes");
        Console.WriteLine($"\nNote: Without proper pool return, this allocates ~272 B/op");
    }

    // ========================================================================
    // GAME LOOP SIMULATION
    // ========================================================================
    
    [Test]
    [Category("GameLoop")]
    public void GameLoop_60FPS_1Minute_SyncHandlers()
    {
        // Use single sync handlers (zero-allocation path)
        var moveHandler = new CountingSyncHandler() as IMessageHandler<GameMessage>;
        var damageHandler = new CountingSyncHandler() as IMessageHandler<GameMessage>;
        var physicsHandler = new CountingSyncHandler() as IMessageHandler<GameMessage>;
        
        var random = new Random(42);
        
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        int gen0Before = GC.CollectionCount(0);
        long allocBefore = GC.GetAllocatedBytesForCurrentThread();
        var sw = Stopwatch.StartNew();
        
        const int FrameCount = 3600;  // 60 FPS * 60 seconds
        int totalMessages = 0;
        
        for (int frame = 0; frame < FrameCount; frame++)
        {
            // 3-5 move messages per frame
            int moves = random.Next(3, 6);
            for (int i = 0; i < moves; i++)
            {
                moveHandler.TryHandle(new GameMessage(frame * 10 + i, random.Next()));
                totalMessages++;
            }
            
            // 10% chance of damage per frame
            if (random.NextDouble() < 0.1)
            {
                damageHandler.TryHandle(new GameMessage(frame, random.Next()));
                totalMessages++;
            }
            
            // Physics every frame
            physicsHandler.TryHandle(new GameMessage(frame, 0.016f));
            totalMessages++;
        }
        
        sw.Stop();
        long allocAfter = GC.GetAllocatedBytesForCurrentThread();
        int gen0After = GC.CollectionCount(0);
        
        long totalAlloc = allocAfter - allocBefore;
        int gcCount = gen0After - gen0Before;
        
        Console.WriteLine($"=== Game Loop: 1 Minute at 60 FPS (Sync Handlers) ===");
        Console.WriteLine($"Frames: {FrameCount}");
        Console.WriteLine($"Total messages: {totalMessages:N0}");
        Console.WriteLine($"Real time: {sw.ElapsedMilliseconds} ms");
        Console.WriteLine($"Time per frame: {sw.ElapsedMilliseconds / (double)FrameCount:F3} ms");
        Console.WriteLine($"Total allocation: {totalAlloc:N0} bytes");
        Console.WriteLine($"Per-message: {totalAlloc / (double)totalMessages:F2} bytes");
        Console.WriteLine($"Gen0 GC collections: {gcCount}");
        
        // Sync handlers should be near zero allocation
        Assert.That(totalAlloc / (double)totalMessages, Is.LessThan(50),
            "Sync handlers should have minimal allocation");
    }

    // ========================================================================
    // CONCURRENCY TESTS
    // ========================================================================
    
    [Test]
    [Category("Concurrency")]
    public async Task HighConcurrency_100Threads_SyncHandlers()
    {
        var handler = new CountingSyncHandler() as IMessageHandler<GameMessage>;
        
        const int ThreadCount = 100;
        const int OpsPerThread = 1000;
        
        var exceptions = new ConcurrentBag<Exception>();
        var sw = Stopwatch.StartNew();
        
        var tasks = Enumerable.Range(0, ThreadCount).Select(threadId => Task.Run(() =>
        {
            try
            {
                for (int i = 0; i < OpsPerThread; i++)
                {
                    handler.TryHandle(new GameMessage(threadId * OpsPerThread + i, i));
                }
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);
            }
        }));
        
        await Task.WhenAll(tasks);
        sw.Stop();
        
        Console.WriteLine($"=== High Concurrency Test (Sync Handlers) ===");
        Console.WriteLine($"Threads: {ThreadCount}");
        Console.WriteLine($"Ops per thread: {OpsPerThread}");
        Console.WriteLine($"Total ops: {ThreadCount * OpsPerThread:N0}");
        Console.WriteLine($"Time: {sw.ElapsedMilliseconds} ms");
        Console.WriteLine($"Throughput: {ThreadCount * OpsPerThread / sw.Elapsed.TotalSeconds:N0} ops/sec");
        Console.WriteLine($"Exceptions: {exceptions.Count}");
        
        Assert.That(exceptions, Is.Empty, "No exceptions should occur under concurrency");
    }

    // ========================================================================
    // STRESS TESTS
    // ========================================================================
    
    [Test]
    [Category("Stress")]
    public void Stress_100K_SyncOperations()
    {
        var handler = new CountingSyncHandler() as IMessageHandler<GameMessage>;
        
        GC.Collect();
        int gen0Before = GC.CollectionCount(0);
        long allocBefore = GC.GetAllocatedBytesForCurrentThread();
        var sw = Stopwatch.StartNew();
        
        for (int i = 0; i < 100_000; i++)
        {
            handler.TryHandle(new GameMessage(i, i));
        }
        
        sw.Stop();
        long allocAfter = GC.GetAllocatedBytesForCurrentThread();
        int gen0After = GC.CollectionCount(0);
        
        Console.WriteLine($"=== 100K Sync Operations ===");
        Console.WriteLine($"Time: {sw.ElapsedMilliseconds} ms");
        Console.WriteLine($"Throughput: {100_000 / sw.Elapsed.TotalSeconds:N0} ops/sec");
        Console.WriteLine($"Total allocation: {(allocAfter - allocBefore):N0} bytes");
        Console.WriteLine($"Per-op: {(allocAfter - allocBefore) / 100_000.0:F2} bytes");
        Console.WriteLine($"Gen0 collections: {gen0After - gen0Before}");
        
        // Should have no GC from messaging with sync handlers
        Assert.That(gen0After - gen0Before, Is.LessThan(5),
            "Sync handlers should cause minimal GC");
    }
    
    [Test]
    [Category("Stress")]
    public void Stress_LargePayloads()
    {
        var handler = new LargePayloadHandler() as IMessageHandler<LargePayloadMessage>;
        
        GC.Collect();
        long allocBefore = GC.GetAllocatedBytesForCurrentThread();
        
        for (int i = 0; i < 1000; i++)
        {
            var positions = new float[1000];  // 4KB payload
            for (int j = 0; j < positions.Length; j++)
                positions[j] = i * 1000 + j;
            
            handler.TryHandle(new LargePayloadMessage(positions));
        }
        
        long allocAfter = GC.GetAllocatedBytesForCurrentThread();
        long totalAlloc = allocAfter - allocBefore;
        long payloadAlloc = 1000 * 1000 * sizeof(float);  // Expected payload allocation
        long frameworkOverhead = totalAlloc - payloadAlloc;
        
        Console.WriteLine($"=== Large Payload Test ===");
        Console.WriteLine($"Operations: 1000 with 4KB payload each");
        Console.WriteLine($"Total allocation: {totalAlloc:N0} bytes");
        Console.WriteLine($"Expected payload: {payloadAlloc:N0} bytes");
        Console.WriteLine($"Framework overhead: {frameworkOverhead:N0} bytes");
        Console.WriteLine($"Per-op overhead: {frameworkOverhead / 1000.0:F2} bytes");
    }

    // ========================================================================
    // LATENCY TESTS
    // ========================================================================
    
    [Test]
    [Category("Latency")]
    public void Latency_Distribution_SyncHandler()
    {
        var handler = new CountingSyncHandler() as IMessageHandler<GameMessage>;
        
        var latencies = new long[10000];
        var sw = new Stopwatch();
        
        // Warm up
        for (int i = 0; i < 100; i++)
        {
            handler.TryHandle(new GameMessage(i, i));
        }
        
        // Measure
        for (int i = 0; i < latencies.Length; i++)
        {
            sw.Restart();
            handler.TryHandle(new GameMessage(i, i));
            sw.Stop();
            latencies[i] = sw.ElapsedTicks;
        }
        
        Array.Sort(latencies);
        
        double ticksPerUs = Stopwatch.Frequency / 1_000_000.0;
        
        Console.WriteLine($"=== Latency Distribution (Sync Handler) ===");
        Console.WriteLine($"Min: {latencies[0] / ticksPerUs:F2} μs");
        Console.WriteLine($"P50: {latencies[5000] / ticksPerUs:F2} μs");
        Console.WriteLine($"P90: {latencies[9000] / ticksPerUs:F2} μs");
        Console.WriteLine($"P95: {latencies[9500] / ticksPerUs:F2} μs");
        Console.WriteLine($"P99: {latencies[9900] / ticksPerUs:F2} μs");
        Console.WriteLine($"Max: {latencies[9999] / ticksPerUs:F2} μs");
        Console.WriteLine($"Avg: {latencies.Average() / ticksPerUs:F2} μs");
    }
}

/// <summary>
/// Tests specifically for pool lifecycle validation.
/// </summary>
[TestFixture]
[Category("PoolLifecycle")]
public class PoolBehaviorTests
{
    public readonly struct TestMsg { public readonly int V; public TestMsg(int v) => V = v; }
    
    private class TestHandler : ISyncMessageHandler<TestMsg>
    {
        public void Handle(TestMsg message) { }
    }
    
    [Test]
    public void PromiseSourcePool_Rent_InitializesProperly()
    {
        var source = TryHandlePromiseSource.Rent();
        Assert.That(source, Is.Not.Null);
        
        source.Init(HandlingResult.Success);
        Assert.That(source.IsCompleted, Is.True);
        Assert.That(source.GetResult().Status, Is.EqualTo(HandlingStatus.Success));
        
        source.Dispose();
    }
    
    [Test]
    public void PromiseSourcePool_RentDisposeRent_Cycles()
    {
        const int Cycles = 100;
        
        for (int i = 0; i < Cycles; i++)
        {
            var source = TryHandlePromiseSource.Rent();
            source.Init(HandlingResult.Success);
            Assert.That(source.GetResult().Status, Is.EqualTo(HandlingStatus.Success));
            source.Dispose();
        }
        
        // After many cycles, pool should be warmed
        GC.Collect();
        long before = GC.GetAllocatedBytesForCurrentThread();
        
        for (int i = 0; i < 64; i++)  // Pool max size
        {
            var source = TryHandlePromiseSource.Rent();
            source.Init(HandlingResult.Success);
            source.Dispose();
        }
        
        long after = GC.GetAllocatedBytesForCurrentThread();
        
        Console.WriteLine($"64 rent/dispose from warm pool: {after - before:N0} bytes");
        
        // Should be minimal allocation with warm pool
        Assert.That((after - before) / 64.0, Is.LessThan(100),
            "Warm pool should minimize allocation");
    }
    
    [Test]
    public async Task PromiseSourcePool_ConcurrentAccess_NoCorruption()
    {
        const int ThreadCount = 20;
        const int OpsPerThread = 500;
        
        var exceptions = new ConcurrentBag<Exception>();
        int successCount = 0;
        
        var tasks = Enumerable.Range(0, ThreadCount).Select(_ => Task.Run(() =>
        {
            try
            {
                for (int i = 0; i < OpsPerThread; i++)
                {
                    var source = TryHandlePromiseSource.Rent();
                    source.Init(HandlingResult.Success);
                    
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
        
        Console.WriteLine($"Concurrent pool access: {successCount} successful, {exceptions.Count} exceptions");
        
        Assert.That(exceptions, Is.Empty);
        Assert.That(successCount, Is.EqualTo(ThreadCount * OpsPerThread));
    }
    
    [Test]
    public void AllPoolTypes_RentAndDispose()
    {
        // TryHandlePromiseSource
        var tryHandle = TryHandlePromiseSource.Rent();
        tryHandle.Init(HandlingResult.Success);
        Assert.That(tryHandle.GetResult().Status, Is.EqualTo(HandlingStatus.Success));
        tryHandle.Dispose();
        
        // TryHandleAsyncPromiseSource
        var tryHandleAsync = TryHandleAsyncPromiseSource.Rent();
        var task = Task.CompletedTask;
        tryHandleAsync.Init(task.GetAwaiter());
        Assert.That(tryHandleAsync.GetResult().Status, Is.EqualTo(HandlingStatus.Success));
        tryHandleAsync.Dispose();
        
        // HandlePromiseSource
        var innerSource = TryHandlePromiseSource.Rent();
        innerSource.Init(HandlingResult.Success);
        var handle = HandlePromiseSource.Rent();
        handle.Init(innerSource);
        Assert.That(handle.GetResult().Status, Is.EqualTo(HandlingStatus.Success));
        handle.Dispose();
        innerSource.Dispose();
        
        // HandleAsyncPromiseSource
        var innerSource2 = TryHandlePromiseSource.Rent();
        innerSource2.Init(HandlingResult.Success);
        var handleAsync = HandleAsyncPromiseSource.Rent();
        handleAsync.Init(innerSource2);
        Assert.That(handleAsync.GetResult().Status, Is.EqualTo(HandlingStatus.Success));
        handleAsync.Dispose();
        innerSource2.Dispose();
        
        Console.WriteLine("All pool types: rent and dispose successful");
    }
    
    [Test]
    public void PreWarm_FillsPools()
    {
        // Pre-warm
        PromiseSourcePools.PreWarm(32);
        
        Assert.That(PromiseSourcePools.IsPreWarmed, Is.True);
        
        // After pre-warm, renting should be near-zero allocation
        GC.Collect();
        long before = GC.GetAllocatedBytesForCurrentThread();
        
        for (int i = 0; i < 32; i++)
        {
            var source = TryHandlePromiseSource.Rent();
            source.Init(HandlingResult.Success);
            source.Dispose();
        }
        
        long after = GC.GetAllocatedBytesForCurrentThread();
        
        Console.WriteLine($"32 ops after PreWarm: {after - before:N0} bytes");
    }
}

/// <summary>
/// GC pressure measurement tests.
/// </summary>
[TestFixture]
[Category("GcPressure")]
public class GcPressureTests
{
    public readonly struct Msg { public readonly int V; public Msg(int v) => V = v; }
    
    private class Handler : ISyncMessageHandler<Msg>
    {
        public void Handle(Msg m) { }
    }
    
    [Test]
    public void MeasureGcCollections_100K_SyncOperations()
    {
        var handler = new Handler() as IMessageHandler<Msg>;
        
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        int gen0Before = GC.CollectionCount(0);
        int gen1Before = GC.CollectionCount(1);
        int gen2Before = GC.CollectionCount(2);
        
        for (int i = 0; i < 100_000; i++)
        {
            handler.TryHandle(new Msg(i));
        }
        
        int gen0After = GC.CollectionCount(0);
        int gen1After = GC.CollectionCount(1);
        int gen2After = GC.CollectionCount(2);
        
        Console.WriteLine($"=== GC Collections for 100K Sync Operations ===");
        Console.WriteLine($"Gen0: {gen0After - gen0Before}");
        Console.WriteLine($"Gen1: {gen1After - gen1Before}");
        Console.WriteLine($"Gen2: {gen2After - gen2Before}");
        
        // Sync handlers with zero-allocation should cause no GC
        Assert.That(gen0After - gen0Before, Is.LessThan(3),
            "Sync handlers should cause minimal Gen0 GC");
    }
    
    [Test]
    public void MeasureGcCollections_Multicast_10K()
    {
        var handler = new MulticastMessageHandler<Msg>();
        var registrar = handler as IMessageHandlerRegistrar<Msg>;
        registrar!.Register(new Handler(), default);
        registrar.Register(new Handler(), default);
        
        // Warm up
        for (int i = 0; i < 100; i++)
        {
            handler.TryHandle(new Msg(i));
        }
        
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        int gen0Before = GC.CollectionCount(0);
        
        for (int i = 0; i < 10_000; i++)
        {
            handler.TryHandle(new Msg(i));
        }
        
        int gen0After = GC.CollectionCount(0);
        
        Console.WriteLine($"=== GC Collections for 10K Multicast Operations ===");
        Console.WriteLine($"Gen0: {gen0After - gen0Before}");
        Console.WriteLine($"\nNote: Multicast currently allocates ~272 B/op without proper pool return");
        Console.WriteLine($"Expected Gen0 collections: ~{(10000 * 272) / (85 * 1024)} (based on 85KB Gen0 budget)");
    }
    
}

/// <summary>
/// Tests for the fire-and-forget API to validate zero-allocation dispatch.
/// </summary>
[TestFixture]
[Category("FireAndForget")]
public class FireAndForgetApiTests
{
    public readonly struct TestMessage 
    { 
        public readonly int Id;
        public readonly float Value;
        
        public TestMessage(int id, float value)
        {
            Id = id;
            Value = value;
        }
    }
    
    private class CountingSyncHandler : ISyncMessageHandler<TestMessage>
    {
        public int HandleCount;
        
        public void Handle(TestMessage message)
        {
            Interlocked.Increment(ref HandleCount);
        }
    }
    
    private class LambdaSyncHandler<T> : ISyncMessageHandler<T>
    {
        private readonly Action<T> _handler;
        
        public LambdaSyncHandler(Action<T> handler) => _handler = handler;
        
        public void Handle(T message) => _handler(message);
    }
    
    [Test]
    [Category("ZeroAllocation")]
    public void TryHandleFireAndForget_ZeroAllocation()
    {
        var handler = new MulticastMessageHandler<TestMessage>();
        var registrar = handler as IMessageHandlerRegistrar<TestMessage>;
        var syncHandler = new CountingSyncHandler();
        registrar!.Register(syncHandler, default);
        registrar.Register(new CountingSyncHandler(), default);
        
        const int Operations = 10_000;
        
        // Warm up
        for (int i = 0; i < 100; i++)
        {
            handler.TryHandleFireAndForget(new TestMessage(i, i * 0.5f));
        }
        
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        long allocBefore = GC.GetAllocatedBytesForCurrentThread();
        
        for (int i = 0; i < Operations; i++)
        {
            handler.TryHandleFireAndForget(new TestMessage(i, i * 0.5f));
        }
        
        long allocAfter = GC.GetAllocatedBytesForCurrentThread();
        long totalAlloc = allocAfter - allocBefore;
        
        Console.WriteLine($"=== TryHandleFireAndForget Zero-Allocation Test ===");
        Console.WriteLine($"{Operations} operations: {totalAlloc:N0} bytes");
        Console.WriteLine($"Per-op: {totalAlloc / (double)Operations:F2} bytes");
        
        Assert.That(totalAlloc / (double)Operations, Is.LessThan(10),
            "TryHandleFireAndForget should be near zero-allocation");
    }
    
    [Test]
    [Category("ZeroAllocation")]
    public void HandleSync_ZeroAllocation()
    {
        var handler = new MulticastMessageHandler<TestMessage>();
        var registrar = handler as IMessageHandlerRegistrar<TestMessage>;
        var syncHandler = new CountingSyncHandler();
        registrar!.Register(syncHandler, default);
        registrar.Register(new CountingSyncHandler(), default);
        
        const int Operations = 10_000;
        
        // Warm up
        for (int i = 0; i < 100; i++)
        {
            _ = handler.HandleSync(new TestMessage(i, i * 0.5f));
        }
        
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        long allocBefore = GC.GetAllocatedBytesForCurrentThread();
        
        for (int i = 0; i < Operations; i++)
        {
            var result = handler.HandleSync(new TestMessage(i, i * 0.5f));
            Assert.That(result.Status, Is.EqualTo(HandlingStatus.Success));
        }
        
        long allocAfter = GC.GetAllocatedBytesForCurrentThread();
        long totalAlloc = allocAfter - allocBefore;
        
        Console.WriteLine($"=== HandleSync Allocation Test ===");
        Console.WriteLine($"{Operations} operations: {totalAlloc:N0} bytes");
        Console.WriteLine($"Per-op: {totalAlloc / (double)Operations:F2} bytes");
        Console.WriteLine($"Note: Some allocation expected from awaitable machinery");
        
        // HandleSync still goes through awaitable machinery internally
        // Main benefit is no promise source pooling overhead
        // Should still be significantly less than unconsumed TryHandle with full pooling
        Assert.That(totalAlloc / (double)Operations, Is.LessThan(600),
            "HandleSync should have lower allocation than full promise pipeline");
    }
    
    [Test]
    public void TryHandleFireAndForget_SwallowsExceptions()
    {
        var handler = new MulticastMessageHandler<TestMessage>();
        var registrar = handler as IMessageHandlerRegistrar<TestMessage>;
        
        int handleCount = 0;
        registrar!.Register(new LambdaSyncHandler<TestMessage>(msg => 
        {
            Interlocked.Increment(ref handleCount);
            throw new InvalidOperationException("Test exception");
        }), default);
        registrar.Register(new LambdaSyncHandler<TestMessage>(msg => 
        {
            Interlocked.Increment(ref handleCount);
        }), default);
        
        // Should not throw
        Assert.DoesNotThrow(() => 
        {
            handler.TryHandleFireAndForget(new TestMessage(1, 1.0f));
        });
        
        // Both handlers should have been called
        Assert.That(handleCount, Is.EqualTo(2));
    }
    
    [Test]
    public void HandleSync_ReturnsAggregatedResult()
    {
        var handler = new MulticastMessageHandler<TestMessage>();
        var registrar = handler as IMessageHandlerRegistrar<TestMessage>;
        registrar!.Register(new CountingSyncHandler(), default);
        registrar.Register(new CountingSyncHandler(), default);
        
        var result = handler.HandleSync(new TestMessage(1, 1.0f));
        
        Assert.That(result.Status, Is.EqualTo(HandlingStatus.Success));
    }
    
    [Test]
    public void HandleSync_AggregatesExceptions()
    {
        var handler = new MulticastMessageHandler<TestMessage>();
        var registrar = handler as IMessageHandlerRegistrar<TestMessage>;
        
        registrar!.Register(new LambdaSyncHandler<TestMessage>(msg => 
        {
            throw new InvalidOperationException("Error 1");
        }), default);
        registrar.Register(new LambdaSyncHandler<TestMessage>(msg => 
        {
            throw new ArgumentException("Error 2");
        }), default);
        
        var result = handler.HandleSync(new TestMessage(1, 1.0f));
        
        Assert.That(result.Status, Is.EqualTo(HandlingStatus.Failure));
        Assert.That(result.Exceptions.Count(), Is.EqualTo(2));
    }
    
    [Test]
    public void HandleSync_NoHandlers_ReturnsNoHandlers()
    {
        var handler = new MulticastMessageHandler<TestMessage>();
        
        var result = handler.HandleSync(new TestMessage(1, 1.0f));
        
        Assert.That(result.Status, Is.EqualTo(HandlingStatus.NotHandled));
    }
    
    [Test]
    public void TryHandleFireAndForget_NoHandlers_DoesNotThrow()
    {
        var handler = new MulticastMessageHandler<TestMessage>();
        
        Assert.DoesNotThrow(() => 
        {
            handler.TryHandleFireAndForget(new TestMessage(1, 1.0f));
        });
    }
    
    [Test]
    public void CompareAllocation_TryHandle_vs_FireAndForget()
    {
        var handler = new MulticastMessageHandler<TestMessage>();
        var registrar = handler as IMessageHandlerRegistrar<TestMessage>;
        registrar!.Register(new CountingSyncHandler(), default);
        registrar.Register(new CountingSyncHandler(), default);
        
        const int Operations = 1000;
        
        // Measure TryHandle (without consuming result)
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        long tryHandleBefore = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < Operations; i++)
        {
            handler.TryHandle(new TestMessage(i, i * 0.5f));
        }
        long tryHandleAlloc = GC.GetAllocatedBytesForCurrentThread() - tryHandleBefore;
        
        // Measure TryHandleFireAndForget
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        
        long fireAndForgetBefore = GC.GetAllocatedBytesForCurrentThread();
        for (int i = 0; i < Operations; i++)
        {
            handler.TryHandleFireAndForget(new TestMessage(i, i * 0.5f));
        }
        long fireAndForgetAlloc = GC.GetAllocatedBytesForCurrentThread() - fireAndForgetBefore;
        
        Console.WriteLine($"=== Allocation Comparison ({Operations} ops) ===");
        Console.WriteLine($"TryHandle (unconsumed):     {tryHandleAlloc:N0} bytes ({tryHandleAlloc / (double)Operations:F1} B/op)");
        Console.WriteLine($"TryHandleFireAndForget:     {fireAndForgetAlloc:N0} bytes ({fireAndForgetAlloc / (double)Operations:F1} B/op)");
        
        // With the sync fast-path optimization, both should be near-zero allocation
        // for sync handlers. FireAndForget should be at most equal to TryHandle.
        Assert.That(fireAndForgetAlloc, Is.LessThanOrEqualTo(tryHandleAlloc),
            "FireAndForget should allocate at most as much as TryHandle");
        
        // Verify both are near-zero (allowing for minor GC noise/measurement variance)
        // The threshold of 200 bytes allows for occasional small allocations from GC internals
        Assert.That(tryHandleAlloc, Is.LessThan(200),
            "TryHandle with sync fast-path should be near-zero allocation");
        Assert.That(fireAndForgetAlloc, Is.LessThan(200),
            "FireAndForget should be near-zero allocation");
    }
}
