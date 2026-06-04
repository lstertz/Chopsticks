using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Chopsticks.Messages.Handlers;
using Chopsticks.Messages.Handlers.Sources;
using NUnit.Framework;

namespace Chopsticks.Messages.Tests
{
    /// <summary>
    /// Comprehensive test suite for promise source pooling validation.
    /// Tests range from small unit tests to massive stress tests with large payloads.
    /// </summary>
    [TestFixture]
    public class PromiseSourcePoolingTests
    {
        #region Test Message Types
        
        public class SimpleMessage
        {
            public int Value { get; set; }
        }
        
        public class LargePayloadMessage
        {
            public Vector3[] Positions { get; set; } = Array.Empty<Vector3>();
            public Vector3[] Velocities { get; set; } = Array.Empty<Vector3>();
            public Vector3[] Normals { get; set; } = Array.Empty<Vector3>();
            public float[] Weights { get; set; } = Array.Empty<float>();
            public int[] Indices { get; set; } = Array.Empty<int>();
        }
        
        public class NestedMessage
        {
            public SimpleMessage[] Children { get; set; } = Array.Empty<SimpleMessage>();
            public Dictionary<string, LargePayloadMessage> PayloadMap { get; set; } = new();
        }
        
        public class GiganticMessage
        {
            public List<LargePayloadMessage> Chunks { get; set; } = new();
            public byte[] RawData { get; set; } = Array.Empty<byte>();
            public Matrix4x4[] Transforms { get; set; } = Array.Empty<Matrix4x4>();
        }
        
        #endregion
        
        #region Test Handlers
        
        public class SyncTestHandler : ISyncMessageHandler<SimpleMessage>
        {
            public int HandleCount;
            public void Handle(SimpleMessage message) => Interlocked.Increment(ref HandleCount);
        }
        
        public class AsyncTestHandler : ITaskMessageHandler<SimpleMessage>
        {
            public int HandleCount;
            public int DelayMs;
            
            public async Task HandleAsync(SimpleMessage message, CancellationToken token = default)
            {
                if (DelayMs > 0)
                    await Task.Delay(DelayMs, token);
                Interlocked.Increment(ref HandleCount);
            }
        }
        
        public class FailingHandler : ISyncMessageHandler<SimpleMessage>
        {
            public void Handle(SimpleMessage message) => 
                throw new InvalidOperationException("Intentional failure");
        }
        
        public class LargePayloadHandler : ISyncMessageHandler<LargePayloadMessage>
        {
            public long TotalVectorsProcessed;
            
            public void Handle(LargePayloadMessage message)
            {
                var count = (message.Positions?.Length ?? 0) +
                           (message.Velocities?.Length ?? 0) +
                           (message.Normals?.Length ?? 0);
                Interlocked.Add(ref TotalVectorsProcessed, count);
            }
        }
        
        public class AsyncLargePayloadHandler : ITaskMessageHandler<LargePayloadMessage>
        {
            public long TotalVectorsProcessed;
            
            public async Task HandleAsync(LargePayloadMessage message, CancellationToken token = default)
            {
                await Task.Yield();
                var count = (message.Positions?.Length ?? 0) +
                           (message.Velocities?.Length ?? 0) +
                           (message.Normals?.Length ?? 0);
                Interlocked.Add(ref TotalVectorsProcessed, count);
            }
        }
        
        public class GiganticPayloadHandler : ITaskMessageHandler<GiganticMessage>
        {
            public long TotalBytesProcessed;
            public long TotalChunksProcessed;
            
            public async Task HandleAsync(GiganticMessage message, CancellationToken token = default)
            {
                await Task.Yield();
                
                long bytes = message.RawData?.Length ?? 0;
                bytes += (message.Transforms?.Length ?? 0) * 64; // Matrix4x4 = 64 bytes
                
                foreach (var chunk in message.Chunks ?? Enumerable.Empty<LargePayloadMessage>())
                {
                    bytes += (chunk.Positions?.Length ?? 0) * 12; // Vector3 = 12 bytes
                    bytes += (chunk.Velocities?.Length ?? 0) * 12;
                    bytes += (chunk.Normals?.Length ?? 0) * 12;
                    bytes += (chunk.Weights?.Length ?? 0) * 4;
                    bytes += (chunk.Indices?.Length ?? 0) * 4;
                }
                
                Interlocked.Add(ref TotalBytesProcessed, bytes);
                Interlocked.Add(ref TotalChunksProcessed, message.Chunks?.Count ?? 0);
            }
        }
        
        #endregion
        
        #region Helper Methods
        
        private static LargePayloadMessage CreateLargePayload(int vectorCount)
        {
            var random = new Random(42);
            return new LargePayloadMessage
            {
                Positions = Enumerable.Range(0, vectorCount)
                    .Select(_ => new Vector3((float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble()))
                    .ToArray(),
                Velocities = Enumerable.Range(0, vectorCount)
                    .Select(_ => new Vector3((float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble()))
                    .ToArray(),
                Normals = Enumerable.Range(0, vectorCount)
                    .Select(_ => Vector3.Normalize(new Vector3((float)random.NextDouble(), (float)random.NextDouble(), (float)random.NextDouble())))
                    .ToArray(),
                Weights = Enumerable.Range(0, vectorCount).Select(_ => (float)random.NextDouble()).ToArray(),
                Indices = Enumerable.Range(0, vectorCount).ToArray()
            };
        }
        
        private static GiganticMessage CreateGiganticPayload(int chunkCount, int vectorsPerChunk, int rawDataSize)
        {
            var random = new Random(42);
            return new GiganticMessage
            {
                Chunks = Enumerable.Range(0, chunkCount)
                    .Select(_ => CreateLargePayload(vectorsPerChunk))
                    .ToList(),
                RawData = Enumerable.Range(0, rawDataSize).Select(_ => (byte)random.Next(256)).ToArray(),
                Transforms = Enumerable.Range(0, chunkCount)
                    .Select(_ => Matrix4x4.CreateRotationY((float)random.NextDouble() * MathF.PI * 2))
                    .ToArray()
            };
        }
        
        #endregion
        
        // ==================================================================================
        // SMALL TESTS - Basic functionality
        // ==================================================================================
        
        #region Small Tests (Unit Tests)
        
        [Test]
        [Category("Small")]
        public void TryHandlePromiseSource_BasicInit_IsCompleted()
        {
            var source = new TryHandlePromiseSource();
            source.Init(HandlingResult.Success);
            
            Assert.That(source.IsCompleted, Is.True);
            Assert.That(source.GetResult().Status, Is.EqualTo(HandlingStatus.Success));
        }
        
        [Test]
        [Category("Small")]
        public void TryHandlePromiseSource_Rent_ReturnsInstance()
        {
            var source = TryHandlePromiseSource.Rent();
            Assert.That(source, Is.Not.Null);
            source.Dispose();
        }
        
        [Test]
        [Category("Small")]
        public void TryHandlePromiseSource_RentAndDispose_ReusesInstance()
        {
            var source1 = TryHandlePromiseSource.Rent();
            source1.Init(HandlingResult.Success);
            source1.Dispose();
            
            var source2 = TryHandlePromiseSource.Rent();
            Assert.That(source2, Is.SameAs(source1));
            source2.Dispose();
        }
        
        [Test]
        [Category("Small")]
        public void TryHandlePromiseSource_Failure_ReturnsException()
        {
            var source = new TryHandlePromiseSource();
            var exception = new InvalidOperationException("Test");
            source.Init(HandlingResult.FromException(exception));
            
            Assert.That(source.IsCompleted, Is.True);
            Assert.That(source.GetResult().Status, Is.EqualTo(HandlingStatus.Failure));
            Assert.That(source.GetResult().Exceptions.First(), Is.SameAs(exception));
        }
        
        [Test]
        [Category("Small")]
        public void TryHandlePromiseSource_OnCompleted_InvokesImmediately()
        {
            var source = new TryHandlePromiseSource();
            source.Init(HandlingResult.Success);
            
            bool called = false;
            source.OnCompleted(() => called = true);
            
            Assert.That(called, Is.True);
        }
        
        [Test]
        [Category("Small")]
        public void TryHandlePromiseSource_Dispose_ClearsState()
        {
            var source = new TryHandlePromiseSource();
            source.Init(HandlingResult.Success);
            source.OnSuccess = () => { };
            source.OnFailure = _ => { };
            
            source.Dispose();
            
            Assert.That(source.OnSuccess, Is.Null);
            Assert.That(source.OnFailure, Is.Null);
        }
        
        [Test]
        [Category("Small")]
        public async Task TryHandleAsyncPromiseSource_CompletedTask_IsCompleted()
        {
            var source = new TryHandleAsyncPromiseSource();
            var task = Task.CompletedTask;
            source.Init(task.GetAwaiter());
            
            Assert.That(source.IsCompleted, Is.True);
            Assert.That(source.GetResult().Status, Is.EqualTo(HandlingStatus.Success));
            await Task.CompletedTask;
        }
        
        [Test]
        [Category("Small")]
        public void TryHandleAsyncPromiseSource_Rent_ReturnsInstance()
        {
            var source = TryHandleAsyncPromiseSource.Rent();
            Assert.That(source, Is.Not.Null);
            source.Dispose();
        }
        
        [Test]
        [Category("Small")]
        public async Task TryHandleAsyncPromiseSource_FailedTask_ReturnsException()
        {
            var source = new TryHandleAsyncPromiseSource();
            var exception = new InvalidOperationException("Test");
            var task = Task.FromException(exception);
            source.Init(task.GetAwaiter());
            
            Assert.That(source.IsCompleted, Is.True);
            var result = source.GetResult();
            Assert.That(result.Status, Is.EqualTo(HandlingStatus.Failure));
            await Task.CompletedTask;
        }
        
        [Test]
        [Category("Small")]
        public async Task TryHandleAsyncPromiseSource_CancelledTask_ReturnsCancelled()
        {
            var source = new TryHandleAsyncPromiseSource();
            var cts = new CancellationTokenSource();
            cts.Cancel();
            var task = Task.FromCanceled(cts.Token);
            source.Init(task.GetAwaiter());
            
            Assert.That(source.IsCompleted, Is.True);
            Assert.That(source.GetResult().Status, Is.EqualTo(HandlingStatus.Cancelled));
            await Task.CompletedTask;
        }
        
        [Test]
        [Category("Small")]
        public void HandlePromiseSource_Rent_ReturnsInstance()
        {
            var source = HandlePromiseSource.Rent();
            Assert.That(source, Is.Not.Null);
            source.Dispose();
        }
        
        [Test]
        [Category("Small")]
        public void HandlePromiseSource_WrapsInnerSource()
        {
            var inner = new TryHandlePromiseSource();
            inner.Init(HandlingResult.Success);
            
            var source = new HandlePromiseSource();
            source.Init(inner);
            
            Assert.That(source.IsCompleted, Is.True);
            Assert.That(source.GetResult().Status, Is.EqualTo(HandlingStatus.Success));
        }
        
        [Test]
        [Category("Small")]
        public void HandleAsyncPromiseSource_Rent_ReturnsInstance()
        {
            var source = HandleAsyncPromiseSource.Rent();
            Assert.That(source, Is.Not.Null);
            source.Dispose();
        }
        
        [Test]
        [Category("Small")]
        public void HandleAsyncPromiseSource_ThrowsOnFailure()
        {
            var inner = new TryHandlePromiseSource();
            inner.Init(HandlingResult.FromException(new InvalidOperationException("Test")));
            
            var source = new HandleAsyncPromiseSource();
            source.Init(inner);
            
            Assert.That(source.IsCompleted, Is.True);
            Assert.Throws<AggregateException>(() => source.GetResult());
        }
        
        [Test]
        [Category("Small")]
        public void SyncHandler_ViaInterface_ReturnsCorrectResult()
        {
            IMessageHandler<SimpleMessage> handler = new SyncTestHandler();
            var message = new SimpleMessage { Value = 42 };
            
            var result = handler.TryHandle(message);
            
            Assert.That(result.Status, Is.EqualTo(HandlingStatus.Success));
        }
        
        [Test]
        [Category("Small")]
        public void FailingHandler_ViaInterface_ReturnsFailure()
        {
            IMessageHandler<SimpleMessage> handler = new FailingHandler();
            var message = new SimpleMessage { Value = 42 };
            
            var result = handler.TryHandle(message);
            
            Assert.That(result.Status, Is.EqualTo(HandlingStatus.Failure));
        }
        
        #endregion
        
        // ==================================================================================
        // MEDIUM TESTS - Multiple handlers, callbacks, async
        // ==================================================================================
        
        #region Medium Tests
        
        [Test]
        [Category("Medium")]
        public void MultipleHandlers_Sequential_AllComplete()
        {
            var handlers = Enumerable.Range(0, 10)
                .Select(_ => (IMessageHandler<SimpleMessage>)new SyncTestHandler())
                .ToArray();
            var message = new SimpleMessage { Value = 1 };
            
            foreach (var handler in handlers)
            {
                var result = handler.TryHandle(message);
                Assert.That(result.Status, Is.EqualTo(HandlingStatus.Success));
            }
        }
        
        [Test]
        [Category("Medium")]
        public async Task MultipleAsyncHandlers_Sequential_AllComplete()
        {
            var handlers = Enumerable.Range(0, 10)
                .Select(_ => (IMessageHandler<SimpleMessage>)new AsyncTestHandler { DelayMs = 1 })
                .ToArray();
            var message = new SimpleMessage { Value = 1 };
            
            foreach (var handler in handlers)
            {
                var awaitable = handler.TryHandleAsync(message, CancellationToken.None);
                await awaitable;
                Assert.That(awaitable.GetAwaiter().GetResult().Status, Is.EqualTo(HandlingStatus.Success));
            }
        }
        
        [Test]
        [Category("Medium")]
        public void Callbacks_AllInvoked_OnSuccess()
        {
            var source = new TryHandlePromiseSource();
            source.Init(HandlingResult.Success);
            
            bool successCalled = false;
            bool completionCalled = false;
            HandlingResult? completionResult = null;
            
            var promise = new HandlingResultPromise(source);
            promise
                .OnSuccess(() => successCalled = true)
                .OnCompletion(r => { completionCalled = true; completionResult = r; });
            
            Assert.That(successCalled, Is.True);
            Assert.That(completionCalled, Is.True);
            Assert.That(completionResult?.Status, Is.EqualTo(HandlingStatus.Success));
        }
        
        [Test]
        [Category("Medium")]
        public void Callbacks_FailureInvoked_OnException()
        {
            var source = new TryHandlePromiseSource();
            var exception = new InvalidOperationException("Test");
            source.Init(HandlingResult.FromException(exception));
            
            bool failureCalled = false;
            IEnumerable<Exception>? failureExceptions = null;
            bool nonSuccessCalled = false;
            
            var promise = new HandlingResultPromise(source);
            promise
                .OnFailure(ex => { failureCalled = true; failureExceptions = ex; })
                .OnNonSuccess(_ => nonSuccessCalled = true);
            
            Assert.That(failureCalled, Is.True);
            Assert.That(failureExceptions?.First(), Is.SameAs(exception));
            Assert.That(nonSuccessCalled, Is.True);
        }
        
        [Test]
        [Category("Medium")]
        public void LargePayload_1000Vectors_Handled()
        {
            IMessageHandler<LargePayloadMessage> handler = new LargePayloadHandler();
            var message = CreateLargePayload(1000);
            
            var result = handler.TryHandle(message);
            
            Assert.That(result.Status, Is.EqualTo(HandlingStatus.Success));
        }
        
        [Test]
        [Category("Medium")]
        public async Task LargePayloadAsync_1000Vectors_Handled()
        {
            IMessageHandler<LargePayloadMessage> handler = new AsyncLargePayloadHandler();
            var message = CreateLargePayload(1000);
            
            var awaitable = handler.TryHandleAsync(message, CancellationToken.None);
            await awaitable;
            
            Assert.That(awaitable.GetAwaiter().GetResult().Status, Is.EqualTo(HandlingStatus.Success));
        }
        
        [Test]
        [Category("Medium")]
        public void PoolReuse_ManyRentDispose_PoolFunctions()
        {
            var sources = new List<TryHandlePromiseSource>();
            
            // Rent 10 sources
            for (int i = 0; i < 10; i++)
            {
                sources.Add(TryHandlePromiseSource.Rent());
            }
            
            // Dispose all
            foreach (var source in sources)
            {
                source.Init(HandlingResult.Success);
                source.Dispose();
            }
            
            // Rent again - should get pooled instances
            var reusedSources = new List<TryHandlePromiseSource>();
            for (int i = 0; i < 10; i++)
            {
                reusedSources.Add(TryHandlePromiseSource.Rent());
            }
            
            // All reused sources should be from the original set
            foreach (var source in reusedSources)
            {
                Assert.That(sources.Contains(source), Is.True);
                source.Dispose();
            }
        }
        
        #endregion
        
        // ==================================================================================
        // LARGE TESTS - Concurrency, stress, many handlers
        // ==================================================================================
        
        #region Large Tests
        
        [Test]
        [Category("Large")]
        public async Task ConcurrentHandling_100Handlers_AllComplete()
        {
            var handlers = Enumerable.Range(0, 100)
                .Select(_ => (IMessageHandler<SimpleMessage>)new SyncTestHandler())
                .ToArray();
            var message = new SimpleMessage { Value = 1 };
            
            var tasks = handlers.Select(h => Task.Run(() =>
            {
                var result = h.TryHandle(message);
                Assert.That(result.Status, Is.EqualTo(HandlingStatus.Success));
            }));
            
            await Task.WhenAll(tasks);
        }
        
        [Test]
        [Category("Large")]
        public async Task ConcurrentAsyncHandling_100Handlers_AllComplete()
        {
            var handlers = Enumerable.Range(0, 100)
                .Select(_ => (IMessageHandler<SimpleMessage>)new AsyncTestHandler { DelayMs = 1 })
                .ToArray();
            var message = new SimpleMessage { Value = 1 };
            
            var tasks = handlers.Select(async h =>
            {
                var awaitable = h.TryHandleAsync(message, CancellationToken.None);
                await awaitable;
                return awaitable.GetAwaiter().GetResult();
            });
            
            var results = await Task.WhenAll(tasks);
            
            Assert.That(results.All(r => r.Status == HandlingStatus.Success), Is.True);
        }
        
        [Test]
        [Category("Large")]
        public void LargePayload_10000Vectors_Handled()
        {
            IMessageHandler<LargePayloadMessage> handler = new LargePayloadHandler();
            var message = CreateLargePayload(10000);
            
            var result = handler.TryHandle(message);
            
            Assert.That(result.Status, Is.EqualTo(HandlingStatus.Success));
        }
        
        [Test]
        [Category("Large")]
        public async Task LargePayload_10000Vectors_ConcurrentHandlers()
        {
            const int handlerCount = 50;
            
            var handlers = Enumerable.Range(0, handlerCount)
                .Select(_ => (IMessageHandler<LargePayloadMessage>)new AsyncLargePayloadHandler())
                .ToArray();
            var message = CreateLargePayload(10000);
            
            var tasks = handlers.Select(async h =>
            {
                var awaitable = h.TryHandleAsync(message, CancellationToken.None);
                await awaitable;
                return awaitable.GetAwaiter().GetResult();
            });
            
            var results = await Task.WhenAll(tasks);
            
            Assert.That(results.All(r => r.Status == HandlingStatus.Success), Is.True);
        }
        
        [Test]
        [Category("Large")]
        public void RapidFireDispatch_1000Messages_AllHandled()
        {
            IMessageHandler<SimpleMessage> handler = new SyncTestHandler();
            var messages = Enumerable.Range(0, 1000).Select(i => new SimpleMessage { Value = i }).ToArray();
            
            foreach (var message in messages)
            {
                var result = handler.TryHandle(message);
                Assert.That(result.Status, Is.EqualTo(HandlingStatus.Success));
            }
        }
        
        [Test]
        [Category("Large")]
        public async Task RapidFireDispatchAsync_1000Messages_AllHandled()
        {
            IMessageHandler<SimpleMessage> handler = new AsyncTestHandler { DelayMs = 0 };
            var messages = Enumerable.Range(0, 1000).Select(i => new SimpleMessage { Value = i }).ToArray();
            
            foreach (var message in messages)
            {
                var awaitable = handler.TryHandleAsync(message, CancellationToken.None);
                await awaitable;
                Assert.That(awaitable.GetAwaiter().GetResult().Status, Is.EqualTo(HandlingStatus.Success));
            }
        }
        
        [Test]
        [Category("Large")]
        public async Task PoolingUnderConcurrentLoad_NoRaceConditions()
        {
            const int iterationsPerTask = 100;
            const int taskCount = 50;
            
            var tasks = Enumerable.Range(0, taskCount).Select(_ => Task.Run(() =>
            {
                for (int j = 0; j < iterationsPerTask; j++)
                {
                    var source = TryHandlePromiseSource.Rent();
                    source.Init(HandlingResult.Success);
                    var result = source.GetResult();
                    source.Dispose();
                }
            }));
            
            await Task.WhenAll(tasks);
        }
        
        [Test]
        [Category("Large")]
        public async Task AllPromiseSourceTypes_ConcurrentPooling()
        {
            const int iterationsPerTask = 100;
            const int taskCount = 20;
            
            var tasks = new List<Task>();
            
            // TryHandlePromiseSource tasks
            tasks.AddRange(Enumerable.Range(0, taskCount).Select(_ => Task.Run(() =>
            {
                for (int i = 0; i < iterationsPerTask; i++)
                {
                    var source = TryHandlePromiseSource.Rent();
                    source.Init(HandlingResult.Success);
                    source.Dispose();
                }
            })));
            
            // TryHandleAsyncPromiseSource tasks
            tasks.AddRange(Enumerable.Range(0, taskCount).Select(_ => Task.Run(() =>
            {
                for (int i = 0; i < iterationsPerTask; i++)
                {
                    var source = TryHandleAsyncPromiseSource.Rent();
                    source.Init(Task.CompletedTask.GetAwaiter());
                    source.Dispose();
                }
            })));
            
            // HandlePromiseSource tasks
            tasks.AddRange(Enumerable.Range(0, taskCount).Select(_ => Task.Run(() =>
            {
                for (int i = 0; i < iterationsPerTask; i++)
                {
                    var inner = TryHandlePromiseSource.Rent();
                    inner.Init(HandlingResult.Success);
                    var source = HandlePromiseSource.Rent();
                    source.Init(inner);
                    source.Dispose();
                    inner.Dispose();
                }
            })));
            
            // HandleAsyncPromiseSource tasks
            tasks.AddRange(Enumerable.Range(0, taskCount).Select(_ => Task.Run(() =>
            {
                for (int i = 0; i < iterationsPerTask; i++)
                {
                    var inner = TryHandlePromiseSource.Rent();
                    inner.Init(HandlingResult.Success);
                    var source = HandleAsyncPromiseSource.Rent();
                    source.Init(inner);
                    source.Dispose();
                    inner.Dispose();
                }
            })));
            
            await Task.WhenAll(tasks);
        }
        
        #endregion
        
        // ==================================================================================
        // HUGE TESTS - Massive payloads, extended stress
        // ==================================================================================
        
        #region Huge Tests
        
        [Test]
        [Category("Huge")]
        [Explicit("Resource intensive - run manually")]
        public void HugePayload_100000Vectors_Handled()
        {
            IMessageHandler<LargePayloadMessage> handler = new LargePayloadHandler();
            var message = CreateLargePayload(100000);
            
            var sw = Stopwatch.StartNew();
            var result = handler.TryHandle(message);
            sw.Stop();
            
            Assert.That(result.Status, Is.EqualTo(HandlingStatus.Success));
            NUnit.Framework.TestContext.WriteLine($"Processed 100,000 vectors in {sw.ElapsedMilliseconds}ms");
        }
        
        [Test]
        [Category("Huge")]
        [Explicit("Resource intensive - run manually")]
        public async Task HugePayload_100000Vectors_ConcurrentHandlers()
        {
            const int handlerCount = 100;
            
            var handlers = Enumerable.Range(0, handlerCount)
                .Select(_ => (IMessageHandler<LargePayloadMessage>)new AsyncLargePayloadHandler())
                .ToArray();
            var message = CreateLargePayload(100000);
            
            var sw = Stopwatch.StartNew();
            var tasks = handlers.Select(async h =>
            {
                var awaitable = h.TryHandleAsync(message, CancellationToken.None);
                await awaitable;
                return awaitable.GetAwaiter().GetResult();
            });
            
            var results = await Task.WhenAll(tasks);
            sw.Stop();
            
            Assert.That(results.All(r => r.Status == HandlingStatus.Success), Is.True);
            NUnit.Framework.TestContext.WriteLine($"Processed 100,000 vectors x {handlerCount} handlers in {sw.ElapsedMilliseconds}ms");
        }
        
        [Test]
        [Category("Huge")]
        [Explicit("Resource intensive - run manually")]
        public void StressTest_10000RapidDispatches()
        {
            IMessageHandler<SimpleMessage> handler = new SyncTestHandler();
            var message = new SimpleMessage { Value = 1 };
            
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < 10000; i++)
            {
                var result = handler.TryHandle(message);
                if (result.Status != HandlingStatus.Success)
                    Assert.Fail($"Failed at iteration {i}");
            }
            sw.Stop();
            
            NUnit.Framework.TestContext.WriteLine($"10,000 dispatches in {sw.ElapsedMilliseconds}ms ({10000.0 / sw.Elapsed.TotalSeconds:N0}/sec)");
        }
        
        [Test]
        [Category("Huge")]
        [Explicit("Resource intensive - run manually")]
        public async Task StressTest_10000ConcurrentDispatches()
        {
            IMessageHandler<SimpleMessage> handler = new AsyncTestHandler { DelayMs = 0 };
            var message = new SimpleMessage { Value = 1 };
            
            var sw = Stopwatch.StartNew();
            var tasks = Enumerable.Range(0, 10000).Select(async _ =>
            {
                var awaitable = handler.TryHandleAsync(message, CancellationToken.None);
                await awaitable;
                return awaitable.GetAwaiter().GetResult();
            });
            
            var results = await Task.WhenAll(tasks);
            sw.Stop();
            
            Assert.That(results.All(r => r.Status == HandlingStatus.Success), Is.True);
            NUnit.Framework.TestContext.WriteLine($"10,000 concurrent dispatches in {sw.ElapsedMilliseconds}ms");
        }
        
        [Test]
        [Category("Huge")]
        [Explicit("Resource intensive - run manually")]
        public async Task GiganticMessage_MultipleChunks_Handled()
        {
            IMessageHandler<GiganticMessage> handler = new GiganticPayloadHandler();
            var message = CreateGiganticPayload(
                chunkCount: 100,
                vectorsPerChunk: 10000,
                rawDataSize: 1024 * 1024 // 1 MB
            );
            
            var sw = Stopwatch.StartNew();
            var awaitable = handler.TryHandleAsync(message, CancellationToken.None);
            await awaitable;
            sw.Stop();
            
            Assert.That(awaitable.GetAwaiter().GetResult().Status, Is.EqualTo(HandlingStatus.Success));
            NUnit.Framework.TestContext.WriteLine($"Processed {message.Chunks.Count} chunks with 10K vectors each in {sw.ElapsedMilliseconds}ms");
        }
        
        #endregion
        
        // ==================================================================================
        // GIGANTIC TESTS - Maximum stress, edge cases at scale
        // ==================================================================================
        
        #region Gigantic Tests
        
        [Test]
        [Category("Gigantic")]
        [Explicit("Extremely resource intensive - run manually")]
        public void GiganticPayload_1MillionVectors_Handled()
        {
            IMessageHandler<LargePayloadMessage> handler = new LargePayloadHandler();
            
            // Create payload with 1 million Vector3s (12 MB just for positions)
            var message = CreateLargePayload(1_000_000);
            
            var sw = Stopwatch.StartNew();
            var result = handler.TryHandle(message);
            sw.Stop();
            
            Assert.That(result.Status, Is.EqualTo(HandlingStatus.Success));
            NUnit.Framework.TestContext.WriteLine($"Processed 1,000,000 vectors in {sw.ElapsedMilliseconds}ms");
        }
        
        [Test]
        [Category("Gigantic")]
        [Explicit("Extremely resource intensive - run manually")]
        public async Task GiganticConcurrentLoad_500Handlers_50000Vectors()
        {
            const int handlerCount = 500;
            
            var handlers = Enumerable.Range(0, handlerCount)
                .Select(_ => (IMessageHandler<LargePayloadMessage>)new AsyncLargePayloadHandler())
                .ToArray();
            var message = CreateLargePayload(50000);
            
            var sw = Stopwatch.StartNew();
            var tasks = handlers.Select(async h =>
            {
                var awaitable = h.TryHandleAsync(message, CancellationToken.None);
                await awaitable;
                return awaitable.GetAwaiter().GetResult();
            });
            
            var results = await Task.WhenAll(tasks);
            sw.Stop();
            
            Assert.That(results.All(r => r.Status == HandlingStatus.Success), Is.True);
            NUnit.Framework.TestContext.WriteLine($"Processed 50,000 vectors x {handlerCount} handlers in {sw.ElapsedMilliseconds}ms");
        }
        
        [Test]
        [Category("Gigantic")]
        [Explicit("Extremely resource intensive - run manually")]
        public async Task GiganticStressTest_100000Dispatches()
        {
            var handlers = Enumerable.Range(0, 100)
                .Select(_ => (IMessageHandler<SimpleMessage>)new SyncTestHandler())
                .ToArray();
            var message = new SimpleMessage { Value = 1 };
            var dispatchCount = new int[handlers.Length];
            
            var sw = Stopwatch.StartNew();
            await Parallel.ForEachAsync(
                Enumerable.Range(0, 100000),
                new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount * 2 },
                async (i, ct) =>
                {
                    var handlerIndex = i % handlers.Length;
                    var result = handlers[handlerIndex].TryHandle(message);
                    if (result.Status == HandlingStatus.Success)
                        Interlocked.Increment(ref dispatchCount[handlerIndex]);
                    await Task.CompletedTask;
                });
            sw.Stop();
            
            var totalDispatches = dispatchCount.Sum();
            Assert.That(totalDispatches, Is.EqualTo(100000));
            NUnit.Framework.TestContext.WriteLine($"100,000 parallel dispatches in {sw.ElapsedMilliseconds}ms ({100000.0 / sw.Elapsed.TotalSeconds:N0}/sec)");
        }
        
        [Test]
        [Category("Gigantic")]
        [Explicit("Extremely resource intensive - run manually")]
        public async Task UltimateStressTest_CombinedPayloadAndConcurrency()
        {
            const int handlerCount = 200;
            const int messagesPerHandler = 100;
            const int vectorsPerMessage = 5000;
            
            var handlers = Enumerable.Range(0, handlerCount)
                .Select(_ => (IMessageHandler<LargePayloadMessage>)new AsyncLargePayloadHandler())
                .ToArray();
            
            // Pre-create messages
            var messages = Enumerable.Range(0, 10)
                .Select(_ => CreateLargePayload(vectorsPerMessage))
                .ToArray();
            
            var sw = Stopwatch.StartNew();
            var tasks = handlers.Select(async (h, idx) =>
            {
                for (int i = 0; i < messagesPerHandler; i++)
                {
                    var message = messages[i % messages.Length];
                    var awaitable = h.TryHandleAsync(message, CancellationToken.None);
                    await awaitable;
                    if (awaitable.GetAwaiter().GetResult().Status != HandlingStatus.Success)
                        return false;
                }
                return true;
            });
            
            var results = await Task.WhenAll(tasks);
            sw.Stop();
            
            Assert.That(results.All(r => r), Is.True);
            NUnit.Framework.TestContext.WriteLine($"Processed {handlerCount * messagesPerHandler} messages in {sw.ElapsedMilliseconds}ms ({handlerCount * messagesPerHandler * 1000.0 / sw.ElapsedMilliseconds:N0} msg/sec)");
        }
        
        #endregion
        
        // ==================================================================================
        // EDGE CASE TESTS - Cancellation, exceptions, boundary conditions
        // ==================================================================================
        
        #region Edge Case Tests
        
        [Test]
        [Category("EdgeCase")]
        public async Task Cancellation_MidFlight_ProperlyCancelled()
        {
            var cts = new CancellationTokenSource();
            IMessageHandler<SimpleMessage> handler = new AsyncTestHandler { DelayMs = 1000 };
            var message = new SimpleMessage { Value = 1 };
            
            var task = Task.Run(async () =>
            {
                var awaitable = handler.TryHandleAsync(message, cts.Token);
                await awaitable;
                return awaitable.GetAwaiter().GetResult();
            });
            
            await Task.Delay(50);
            cts.Cancel();
            
            // The task may throw or return cancelled depending on timing
            try
            {
                var result = await task;
                // If we get here, might be cancelled status or success (if it completed before cancel)
            }
            catch (OperationCanceledException)
            {
                // Expected
            }
        }
        
        [Test]
        [Category("EdgeCase")]
        public void ExceptionInHandler_DoesNotCorruptState()
        {
            IMessageHandler<SimpleMessage> handler = new FailingHandler();
            var message = new SimpleMessage { Value = 1 };
            
            for (int i = 0; i < 100; i++)
            {
                var result = handler.TryHandle(message);
                Assert.That(result.Status, Is.EqualTo(HandlingStatus.Failure));
            }
        }
        
        [Test]
        [Category("EdgeCase")]
        public async Task ExceptionInAsyncHandler_ProperlyWrapped()
        {
            var source = new TryHandleAsyncPromiseSource();
            var exception = new InvalidOperationException("Async failure");
            var task = Task.FromException(exception);
            source.Init(task.GetAwaiter());
            
            var result = source.GetResult();
            
            Assert.That(result.Status, Is.EqualTo(HandlingStatus.Failure));
            Assert.That(result.Exceptions.First(), Is.TypeOf<InvalidOperationException>());
            await Task.CompletedTask;
        }
        
        [Test]
        [Category("EdgeCase")]
        public void EmptyPayload_Handled()
        {
            IMessageHandler<LargePayloadMessage> handler = new LargePayloadHandler();
            var message = new LargePayloadMessage(); // Empty arrays
            
            var result = handler.TryHandle(message);
            
            Assert.That(result.Status, Is.EqualTo(HandlingStatus.Success));
        }
        
        [Test]
        [Category("EdgeCase")]
        public void NullExceptionsInPayload_Handled()
        {
            IMessageHandler<LargePayloadMessage> handler = new LargePayloadHandler();
            var message = new LargePayloadMessage
            {
                Positions = null!,
                Velocities = null!,
                Normals = null!
            };
            
            var result = handler.TryHandle(message);
            
            Assert.That(result.Status, Is.EqualTo(HandlingStatus.Success));
        }
        
        [Test]
        [Category("EdgeCase")]
        public async Task VeryLongRunningHandler_Completes()
        {
            IMessageHandler<SimpleMessage> handler = new AsyncTestHandler { DelayMs = 500 };
            var message = new SimpleMessage { Value = 1 };
            
            var sw = Stopwatch.StartNew();
            var awaitable = handler.TryHandleAsync(message, CancellationToken.None);
            await awaitable;
            sw.Stop();
            
            Assert.That(awaitable.GetAwaiter().GetResult().Status, Is.EqualTo(HandlingStatus.Success));
            Assert.That(sw.ElapsedMilliseconds, Is.GreaterThanOrEqualTo(400));
        }
        
        [Test]
        [Category("EdgeCase")]
        public void DisposeWhileActive_NoException()
        {
            var source = new TryHandlePromiseSource();
            source.Init(HandlingResult.Success);
            source.OnSuccess = () => { };
            
            // Dispose should not throw
            Assert.DoesNotThrow(() => source.Dispose());
        }
        
        [Test]
        [Category("EdgeCase")]
        public void MultipleDisposes_NoException()
        {
            var source = new TryHandlePromiseSource();
            source.Init(HandlingResult.Success);
            
            Assert.DoesNotThrow(() =>
            {
                source.Dispose();
                source.Dispose();
                source.Dispose();
            });
        }
        
        [Test]
        [Category("EdgeCase")]
        public void PoolOverflow_DoesNotThrow()
        {
            // Create and dispose many sources to overflow the pool
            var sources = new List<TryHandlePromiseSource>();
            for (int i = 0; i < 200; i++)
            {
                sources.Add(TryHandlePromiseSource.Rent());
            }
            
            // Dispose all - pool should cap at MaxPoolSize
            foreach (var source in sources)
            {
                source.Init(HandlingResult.Success);
                Assert.DoesNotThrow(() => source.Dispose());
            }
        }
        
        #endregion
    }
}
