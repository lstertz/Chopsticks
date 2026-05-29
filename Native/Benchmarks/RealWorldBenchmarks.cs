using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using Chopsticks.Messages;
using Chopsticks.Messages.Handlers;
using Chopsticks.Messages.Handlers.Multicast;
using Chopsticks.Messages.Handlers.Sources;
using Chopsticks.Messages.Registration;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Chopsticks.Benchmarks;

/// <summary>
/// Real-world benchmark scenarios that simulate actual usage patterns.
/// These benchmarks DO NOT manually call Dispose() to mirror production behavior.
/// </summary>
[Config(typeof(RealWorldConfig))]
[MemoryDiagnoser]
[GcServer(true)]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
public class RealWorldBenchmarks
{
    private class RealWorldConfig : ManualConfig
    {
        public RealWorldConfig()
        {
            AddJob(Job.Default
                .WithWarmupCount(3)
                .WithIterationCount(10)
                .WithInvocationCount(1)  // Single invocation for long-running tests
                .WithUnrollFactor(1));
            
            AddDiagnoser(MemoryDiagnoser.Default);
        }
    }

    // ========================================================================
    // Test Messages
    // ========================================================================
    
    public readonly struct PlayerMoveMessage
    {
        public readonly int PlayerId;
        public readonly float X, Y, Z;
        public readonly float DeltaTime;
        
        public PlayerMoveMessage(int playerId, float x, float y, float z, float dt)
        {
            PlayerId = playerId;
            X = x; Y = y; Z = z;
            DeltaTime = dt;
        }
    }
    
    public readonly struct DamageMessage
    {
        public readonly int SourceId;
        public readonly int TargetId;
        public readonly float Amount;
        
        public DamageMessage(int source, int target, float amount)
        {
            SourceId = source;
            TargetId = target;
            Amount = amount;
        }
    }
    
    public readonly struct NetworkSyncMessage
    {
        public readonly byte[] Payload;
        
        public NetworkSyncMessage(byte[] payload) => Payload = payload;
    }
    
    public readonly struct PhysicsTickMessage
    {
        public readonly float DeltaTime;
        public readonly int TickNumber;
        
        public PhysicsTickMessage(float dt, int tick)
        {
            DeltaTime = dt;
            TickNumber = tick;
        }
    }
    
    public readonly struct UIUpdateMessage
    {
        public readonly string ElementId;
        public readonly object Value;
        
        public UIUpdateMessage(string id, object value)
        {
            ElementId = id;
            Value = value;
        }
    }

    // ========================================================================
    // Sync Handlers (Common in games)
    // ========================================================================
    
    private class SyncPlayerMoveHandler : ISyncMessageHandler<PlayerMoveMessage>
    {
        private float _totalDistance;
        
        public void Handle(PlayerMoveMessage message)
        {
            _totalDistance += MathF.Sqrt(
                message.X * message.X + 
                message.Y * message.Y + 
                message.Z * message.Z);
        }
    }
    
    private class SyncDamageHandler : ISyncMessageHandler<DamageMessage>
    {
        private readonly Dictionary<int, float> _healthPool = new(100);
        
        public void Handle(DamageMessage message)
        {
            if (!_healthPool.TryGetValue(message.TargetId, out var health))
                health = 100f;
            
            _healthPool[message.TargetId] = health - message.Amount;
        }
    }
    
    private class SyncPhysicsHandler : ISyncMessageHandler<PhysicsTickMessage>
    {
        private int _lastTick;
        
        public void Handle(PhysicsTickMessage message)
        {
            _lastTick = message.TickNumber;
            Thread.SpinWait(10);  // Simulate minimal physics work
        }
    }

    // ========================================================================
    // Async Handlers (Common in servers)
    // ========================================================================
    
    private class AsyncNetworkHandler : ITaskMessageHandler<NetworkSyncMessage>
    {
        public async Task HandleAsync(NetworkSyncMessage message, CancellationToken token = default)
        {
            await Task.Yield();  // Simulate async I/O
        }
    }
    
    private class AsyncUIHandler : ITaskMessageHandler<UIUpdateMessage>
    {
        public async Task HandleAsync(UIUpdateMessage message, CancellationToken token = default)
        {
            await Task.Delay(1, token);  // Simulate UI thread marshaling
        }
    }

    // ========================================================================
    // Multicast Handlers
    // ========================================================================
    
    private MulticastMessageHandler<PlayerMoveMessage> _moveMulticast = null!;
    private MulticastMessageHandler<DamageMessage> _damageMulticast = null!;
    private MulticastMessageHandler<PhysicsTickMessage> _physicsMulticast = null!;
    private MulticastMessageHandler<NetworkSyncMessage> _networkMulticast = null!;
    
    private IMessageHandler<PlayerMoveMessage> _syncMoveHandler = null!;
    private IMessageHandler<DamageMessage> _syncDamageHandler = null!;
    private IMessageHandler<PhysicsTickMessage> _syncPhysicsHandler = null!;

    [GlobalSetup]
    public void Setup()
    {
        // DO NOT pre-warm pools - we want to measure real-world behavior
        // PromiseSourcePools.PreWarm();  // Intentionally commented out
        
        // Single sync handlers
        _syncMoveHandler = new SyncPlayerMoveHandler();
        _syncDamageHandler = new SyncDamageHandler();
        _syncPhysicsHandler = new SyncPhysicsHandler();
        
        // Multicast with multiple handlers
        _moveMulticast = new MulticastMessageHandler<PlayerMoveMessage>();
        ((IMessageHandlerRegistrar<PlayerMoveMessage>)_moveMulticast).Register(new SyncPlayerMoveHandler(), default);
        ((IMessageHandlerRegistrar<PlayerMoveMessage>)_moveMulticast).Register(new SyncPlayerMoveHandler(), default);
        ((IMessageHandlerRegistrar<PlayerMoveMessage>)_moveMulticast).Register(new SyncPlayerMoveHandler(), default);
        
        _damageMulticast = new MulticastMessageHandler<DamageMessage>();
        ((IMessageHandlerRegistrar<DamageMessage>)_damageMulticast).Register(new SyncDamageHandler(), default);
        ((IMessageHandlerRegistrar<DamageMessage>)_damageMulticast).Register(new SyncDamageHandler(), default);
        
        _physicsMulticast = new MulticastMessageHandler<PhysicsTickMessage>();
        ((IMessageHandlerRegistrar<PhysicsTickMessage>)_physicsMulticast).Register(new SyncPhysicsHandler(), default);
        ((IMessageHandlerRegistrar<PhysicsTickMessage>)_physicsMulticast).Register(new SyncPhysicsHandler(), default);
        ((IMessageHandlerRegistrar<PhysicsTickMessage>)_physicsMulticast).Register(new SyncPhysicsHandler(), default);
        ((IMessageHandlerRegistrar<PhysicsTickMessage>)_physicsMulticast).Register(new SyncPhysicsHandler(), default);
        
        _networkMulticast = new MulticastMessageHandler<NetworkSyncMessage>();
        ((IMessageHandlerRegistrar<NetworkSyncMessage>)_networkMulticast).Register(new AsyncNetworkHandler(), default);
    }

    // ========================================================================
    // BENCHMARK: Game Loop Simulation (60 FPS, 10 seconds)
    // ========================================================================
    
    [Benchmark(Description = "GameLoop_60FPS_10Seconds")]
    public void GameLoop_60FPS_10Seconds()
    {
        const int FrameCount = 600;  // 10 seconds at 60 FPS
        const float FrameTime = 1f / 60f;
        
        var random = new Random(42);
        
        for (int frame = 0; frame < FrameCount; frame++)
        {
            // Typical game frame: multiple message types
            
            // Player movement (2-5 players moving per frame)
            int movingPlayers = random.Next(2, 6);
            for (int i = 0; i < movingPlayers; i++)
            {
                var move = new PlayerMoveMessage(
                    random.Next(100),
                    (float)random.NextDouble() * 10,
                    (float)random.NextDouble() * 10,
                    (float)random.NextDouble() * 10,
                    FrameTime);
                
                // NO Dispose() called - mirrors production
                _syncMoveHandler.TryHandle(move);
            }
            
            // Damage events (0-2 per frame on average)
            if (random.NextDouble() < 0.3)
            {
                var damage = new DamageMessage(
                    random.Next(100),
                    random.Next(100),
                    (float)random.NextDouble() * 50);
                
                _syncDamageHandler.TryHandle(damage);
            }
            
            // Physics tick (every frame)
            var physics = new PhysicsTickMessage(FrameTime, frame);
            _syncPhysicsHandler.TryHandle(physics);
        }
    }

    // ========================================================================
    // BENCHMARK: Game Loop with Multicast (More Realistic)
    // ========================================================================
    
    [Benchmark(Description = "GameLoop_Multicast_60FPS_10Seconds")]
    public void GameLoop_Multicast_60FPS_10Seconds()
    {
        const int FrameCount = 600;
        const float FrameTime = 1f / 60f;
        
        var random = new Random(42);
        
        for (int frame = 0; frame < FrameCount; frame++)
        {
            // Multiple handlers per message type (multicast)
            int movingPlayers = random.Next(2, 6);
            for (int i = 0; i < movingPlayers; i++)
            {
                var move = new PlayerMoveMessage(
                    random.Next(100),
                    (float)random.NextDouble() * 10,
                    (float)random.NextDouble() * 10,
                    (float)random.NextDouble() * 10,
                    FrameTime);
                
                // Multicast to 3 handlers - NO Dispose()
                _moveMulticast.TryHandle(move);
            }
            
            if (random.NextDouble() < 0.3)
            {
                var damage = new DamageMessage(
                    random.Next(100),
                    random.Next(100),
                    (float)random.NextDouble() * 50);
                
                _damageMulticast.TryHandle(damage);
            }
            
            var physics = new PhysicsTickMessage(FrameTime, frame);
            _physicsMulticast.TryHandle(physics);
        }
    }

    // ========================================================================
    // BENCHMARK: Server Workload (High Concurrency)
    // ========================================================================
    
    [Benchmark(Description = "Server_1000Requests_50Concurrent")]
    public async Task Server_1000Requests_50Concurrent()
    {
        const int TotalRequests = 1000;
        const int Concurrency = 50;
        
        var semaphore = new SemaphoreSlim(Concurrency);
        var tasks = new List<Task>(TotalRequests);
        var random = new Random(42);
        
        for (int i = 0; i < TotalRequests; i++)
        {
            await semaphore.WaitAsync();
            
            var payload = new byte[random.Next(100, 1000)];
            random.NextBytes(payload);
            var message = new NetworkSyncMessage(payload);
            
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    // Async handler - NO Dispose()
                    await _networkMulticast.TryHandleAsync(message);
                }
                finally
                {
                    semaphore.Release();
                }
            }));
        }
        
        await Task.WhenAll(tasks);
    }

    // ========================================================================
    // BENCHMARK: Sustained Load (Pool Drain Test)
    // ========================================================================
    
    [Benchmark(Description = "PoolDrain_10000Ops_NoDispose")]
    public void PoolDrain_10000Ops_NoDispose()
    {
        // This test specifically measures what happens when pools drain
        // because Dispose() is never called (real-world behavior)
        
        for (int i = 0; i < 10000; i++)
        {
            var move = new PlayerMoveMessage(i, i, i, i, 0.016f);
            
            // Single sync handler - should be 0 B with V2.1 direct result
            _syncMoveHandler.TryHandle(move);
        }
    }

    [Benchmark(Description = "PoolDrain_Multicast_10000Ops_NoDispose")]
    public void PoolDrain_Multicast_10000Ops_NoDispose()
    {
        // Multicast path - will show pool drain effect
        
        for (int i = 0; i < 10000; i++)
        {
            var physics = new PhysicsTickMessage(0.016f, i);
            
            // Multicast to 4 handlers - allocates SequentialHandlingPromiseSource
            // Pool will drain after 64 ops, then allocate fresh each time
            _physicsMulticast.TryHandle(physics);
        }
    }

    // ========================================================================
    // BENCHMARK: Burst Traffic (Spike Handling)
    // ========================================================================
    
    [Benchmark(Description = "Burst_1000Messages_Instant")]
    public void Burst_1000Messages_Instant()
    {
        // Simulates traffic spike (e.g., battle start, zone load)
        
        var messages = new PlayerMoveMessage[1000];
        for (int i = 0; i < 1000; i++)
        {
            messages[i] = new PlayerMoveMessage(i % 100, i, i, i, 0.016f);
        }
        
        // Burst all at once
        for (int i = 0; i < messages.Length; i++)
        {
            _moveMulticast.TryHandle(messages[i]);
        }
    }

    // ========================================================================
    // BENCHMARK: Mixed Sync/Async Workload
    // ========================================================================
    
    [Benchmark(Description = "Mixed_SyncAsync_1000Ops")]
    public async Task Mixed_SyncAsync_1000Ops()
    {
        var random = new Random(42);
        
        for (int i = 0; i < 1000; i++)
        {
            // 70% sync, 30% async (typical game server)
            if (random.NextDouble() < 0.7)
            {
                var move = new PlayerMoveMessage(i, i, i, i, 0.016f);
                _syncMoveHandler.TryHandle(move);
            }
            else
            {
                var network = new NetworkSyncMessage(new byte[100]);
                await _networkMulticast.TryHandleAsync(network);
            }
        }
    }
}

/// <summary>
/// Long-running memory tests that measure allocation patterns over time.
/// These are designed to detect memory leaks and pool drain.
/// </summary>
[MemoryDiagnoser]
public class MemoryGrowthBenchmarks
{
    private MulticastMessageHandler<RealWorldBenchmarks.PhysicsTickMessage> _multicast = null!;
    
    private class SimplePhysicsHandler : ISyncMessageHandler<RealWorldBenchmarks.PhysicsTickMessage>
    {
        public void Handle(RealWorldBenchmarks.PhysicsTickMessage message) { }
    }

    [GlobalSetup]
    public void Setup()
    {
        _multicast = new MulticastMessageHandler<RealWorldBenchmarks.PhysicsTickMessage>();
        ((IMessageHandlerRegistrar<RealWorldBenchmarks.PhysicsTickMessage>)_multicast).Register(new SimplePhysicsHandler(), default);
        ((IMessageHandlerRegistrar<RealWorldBenchmarks.PhysicsTickMessage>)_multicast).Register(new SimplePhysicsHandler(), default);
    }

    [Benchmark(Description = "First64Ops_PoolFilling")]
    public void First64Ops_PoolFilling()
    {
        // First 64 operations - pool should be filling
        for (int i = 0; i < 64; i++)
        {
            _multicast.TryHandle(new RealWorldBenchmarks.PhysicsTickMessage(0.016f, i));
        }
    }

    [Benchmark(Description = "Next1000Ops_PoolDrained")]
    public void Next1000Ops_PoolDrained()
    {
        // After pool is full (64), these should allocate fresh
        // unless auto-return is implemented
        for (int i = 0; i < 1000; i++)
        {
            _multicast.TryHandle(new RealWorldBenchmarks.PhysicsTickMessage(0.016f, i));
        }
    }

    [Benchmark(Description = "Comparison_FireAndForget")]
    public void Comparison_FireAndForget()
    {
        // Using fire-and-forget API for zero-allocation dispatch
        for (int i = 0; i < 1000; i++)
        {
            _multicast.TryHandleFireAndForget(new RealWorldBenchmarks.PhysicsTickMessage(0.016f, i));
        }
    }
}

/// <summary>
/// GC pressure measurement benchmarks.
/// </summary>
public class GcPressureBenchmarks
{
    private MulticastMessageHandler<RealWorldBenchmarks.PhysicsTickMessage> _multicast = null!;
    private IMessageHandler<RealWorldBenchmarks.PhysicsTickMessage> _syncHandler = null!;
    
    private class SimplePhysicsHandler : ISyncMessageHandler<RealWorldBenchmarks.PhysicsTickMessage>
    {
        public void Handle(RealWorldBenchmarks.PhysicsTickMessage message) { }
    }

    [GlobalSetup]
    public void Setup()
    {
        _syncHandler = new SimplePhysicsHandler();
        
        _multicast = new MulticastMessageHandler<RealWorldBenchmarks.PhysicsTickMessage>();
        ((IMessageHandlerRegistrar<RealWorldBenchmarks.PhysicsTickMessage>)_multicast).Register(new SimplePhysicsHandler(), default);
        ((IMessageHandlerRegistrar<RealWorldBenchmarks.PhysicsTickMessage>)_multicast).Register(new SimplePhysicsHandler(), default);
    }

    [Benchmark(Description = "GC_Sync_100K_Ops")]
    public (int gen0, int gen1, int gen2) GC_Sync_100K_Ops()
    {
        int gen0Before = GC.CollectionCount(0);
        int gen1Before = GC.CollectionCount(1);
        int gen2Before = GC.CollectionCount(2);
        
        for (int i = 0; i < 100_000; i++)
        {
            _syncHandler.TryHandle(new RealWorldBenchmarks.PhysicsTickMessage(0.016f, i));
        }
        
        return (
            GC.CollectionCount(0) - gen0Before,
            GC.CollectionCount(1) - gen1Before,
            GC.CollectionCount(2) - gen2Before
        );
    }

    [Benchmark(Description = "GC_Multicast_100K_Ops")]
    public (int gen0, int gen1, int gen2) GC_Multicast_100K_Ops()
    {
        int gen0Before = GC.CollectionCount(0);
        int gen1Before = GC.CollectionCount(1);
        int gen2Before = GC.CollectionCount(2);
        
        for (int i = 0; i < 100_000; i++)
        {
            _multicast.TryHandle(new RealWorldBenchmarks.PhysicsTickMessage(0.016f, i));
        }
        
        return (
            GC.CollectionCount(0) - gen0Before,
            GC.CollectionCount(1) - gen1Before,
            GC.CollectionCount(2) - gen2Before
        );
    }
}

/// <summary>
/// Latency distribution benchmarks for tail latency analysis.
/// </summary>
public class LatencyDistributionBenchmarks
{
    private IMessageHandler<RealWorldBenchmarks.PhysicsTickMessage> _syncHandler = null!;
    private MulticastMessageHandler<RealWorldBenchmarks.PhysicsTickMessage> _multicast = null!;
    
    private class SimplePhysicsHandler : ISyncMessageHandler<RealWorldBenchmarks.PhysicsTickMessage>
    {
        public void Handle(RealWorldBenchmarks.PhysicsTickMessage message) { }
    }

    [GlobalSetup]
    public void Setup()
    {
        _syncHandler = new SimplePhysicsHandler();
        
        _multicast = new MulticastMessageHandler<RealWorldBenchmarks.PhysicsTickMessage>();
        ((IMessageHandlerRegistrar<RealWorldBenchmarks.PhysicsTickMessage>)_multicast).Register(new SimplePhysicsHandler(), default);
        ((IMessageHandlerRegistrar<RealWorldBenchmarks.PhysicsTickMessage>)_multicast).Register(new SimplePhysicsHandler(), default);
    }

    [Benchmark(Description = "Latency_Sync_10K")]
    public long[] Latency_Sync_10K()
    {
        var latencies = new long[10_000];
        var sw = new Stopwatch();
        
        for (int i = 0; i < latencies.Length; i++)
        {
            sw.Restart();
            _syncHandler.TryHandle(new RealWorldBenchmarks.PhysicsTickMessage(0.016f, i));
            sw.Stop();
            latencies[i] = sw.ElapsedTicks;
        }
        
        Array.Sort(latencies);
        return latencies;  // Can analyze p50, p95, p99 from sorted array
    }

    [Benchmark(Description = "Latency_Multicast_10K")]
    public long[] Latency_Multicast_10K()
    {
        var latencies = new long[10_000];
        var sw = new Stopwatch();
        
        for (int i = 0; i < latencies.Length; i++)
        {
            sw.Restart();
            _multicast.TryHandle(new RealWorldBenchmarks.PhysicsTickMessage(0.016f, i));
            sw.Stop();
            latencies[i] = sw.ElapsedTicks;
        }
        
        Array.Sort(latencies);
        return latencies;
    }
}
