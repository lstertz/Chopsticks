using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Chopsticks.Messages;
using Chopsticks.Messages.Handlers;
using Chopsticks.Messages.Handlers.Sources;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Chopsticks.Benchmarks
{
    /// <summary>
    /// Benchmarks to measure the allocation savings from pooling promise sources.
    /// </summary>
    [MemoryDiagnoser]
    [SimpleJob(warmupCount: 3, iterationCount: 10)]
    public class PromiseSourcePoolingBenchmarks
    {
        private const int SmallIterations = 1000;
        private const int MediumIterations = 10000;
        private const int LargeIterations = 100000;

        #region TryHandlePromiseSource Benchmarks
        
        [Benchmark(Description = "TryHandlePromiseSource - Pooled (1K ops)")]
        [BenchmarkCategory("TryHandle", "Small")]
        public void TryHandlePooled_Small()
        {
            for (int i = 0; i < SmallIterations; i++)
            {
                var source = TryHandlePromiseSource.Rent();
                source.Init(HandlingResult.Success);
                _ = source.GetResult();
                source.Dispose();
            }
        }
        
        [Benchmark(Description = "TryHandlePromiseSource - New (1K ops)")]
        [BenchmarkCategory("TryHandle", "Small")]
        public void TryHandleNew_Small()
        {
            for (int i = 0; i < SmallIterations; i++)
            {
                var source = new TryHandlePromiseSource();
                source.Init(HandlingResult.Success);
                _ = source.GetResult();
            }
        }
        
        [Benchmark(Description = "TryHandlePromiseSource - Pooled (10K ops)")]
        [BenchmarkCategory("TryHandle", "Medium")]
        public void TryHandlePooled_Medium()
        {
            for (int i = 0; i < MediumIterations; i++)
            {
                var source = TryHandlePromiseSource.Rent();
                source.Init(HandlingResult.Success);
                _ = source.GetResult();
                source.Dispose();
            }
        }
        
        [Benchmark(Description = "TryHandlePromiseSource - New (10K ops)")]
        [BenchmarkCategory("TryHandle", "Medium")]
        public void TryHandleNew_Medium()
        {
            for (int i = 0; i < MediumIterations; i++)
            {
                var source = new TryHandlePromiseSource();
                source.Init(HandlingResult.Success);
                _ = source.GetResult();
            }
        }
        
        [Benchmark(Description = "TryHandlePromiseSource - Pooled (100K ops)")]
        [BenchmarkCategory("TryHandle", "Large")]
        public void TryHandlePooled_Large()
        {
            for (int i = 0; i < LargeIterations; i++)
            {
                var source = TryHandlePromiseSource.Rent();
                source.Init(HandlingResult.Success);
                _ = source.GetResult();
                source.Dispose();
            }
        }
        
        [Benchmark(Description = "TryHandlePromiseSource - New (100K ops)")]
        [BenchmarkCategory("TryHandle", "Large")]
        public void TryHandleNew_Large()
        {
            for (int i = 0; i < LargeIterations; i++)
            {
                var source = new TryHandlePromiseSource();
                source.Init(HandlingResult.Success);
                _ = source.GetResult();
            }
        }
        
        #endregion
        
        #region TryHandleAsyncPromiseSource Benchmarks
        
        [Benchmark(Description = "TryHandleAsyncPromiseSource - Pooled (1K ops)")]
        [BenchmarkCategory("TryHandleAsync", "Small")]
        public void TryHandleAsyncPooled_Small()
        {
            var completedTask = Task.CompletedTask;
            for (int i = 0; i < SmallIterations; i++)
            {
                var source = TryHandleAsyncPromiseSource.Rent();
                source.Init(completedTask.GetAwaiter());
                _ = source.GetResult();
                source.Dispose();
            }
        }
        
        [Benchmark(Description = "TryHandleAsyncPromiseSource - New (1K ops)")]
        [BenchmarkCategory("TryHandleAsync", "Small")]
        public void TryHandleAsyncNew_Small()
        {
            var completedTask = Task.CompletedTask;
            for (int i = 0; i < SmallIterations; i++)
            {
                var source = new TryHandleAsyncPromiseSource();
                source.Init(completedTask.GetAwaiter());
                _ = source.GetResult();
            }
        }
        
        [Benchmark(Description = "TryHandleAsyncPromiseSource - Pooled (10K ops)")]
        [BenchmarkCategory("TryHandleAsync", "Medium")]
        public void TryHandleAsyncPooled_Medium()
        {
            var completedTask = Task.CompletedTask;
            for (int i = 0; i < MediumIterations; i++)
            {
                var source = TryHandleAsyncPromiseSource.Rent();
                source.Init(completedTask.GetAwaiter());
                _ = source.GetResult();
                source.Dispose();
            }
        }
        
        [Benchmark(Description = "TryHandleAsyncPromiseSource - New (10K ops)")]
        [BenchmarkCategory("TryHandleAsync", "Medium")]
        public void TryHandleAsyncNew_Medium()
        {
            var completedTask = Task.CompletedTask;
            for (int i = 0; i < MediumIterations; i++)
            {
                var source = new TryHandleAsyncPromiseSource();
                source.Init(completedTask.GetAwaiter());
                _ = source.GetResult();
            }
        }
        
        [Benchmark(Description = "TryHandleAsyncPromiseSource - Pooled (100K ops)")]
        [BenchmarkCategory("TryHandleAsync", "Large")]
        public void TryHandleAsyncPooled_Large()
        {
            var completedTask = Task.CompletedTask;
            for (int i = 0; i < LargeIterations; i++)
            {
                var source = TryHandleAsyncPromiseSource.Rent();
                source.Init(completedTask.GetAwaiter());
                _ = source.GetResult();
                source.Dispose();
            }
        }
        
        [Benchmark(Description = "TryHandleAsyncPromiseSource - New (100K ops)")]
        [BenchmarkCategory("TryHandleAsync", "Large")]
        public void TryHandleAsyncNew_Large()
        {
            var completedTask = Task.CompletedTask;
            for (int i = 0; i < LargeIterations; i++)
            {
                var source = new TryHandleAsyncPromiseSource();
                source.Init(completedTask.GetAwaiter());
                _ = source.GetResult();
            }
        }
        
        #endregion
        
        #region HandlePromiseSource Benchmarks
        
        private static readonly IHandlingPromiseSource _innerSuccess;
        
        static PromiseSourcePoolingBenchmarks()
        {
            var source = new TryHandlePromiseSource();
            source.Init(HandlingResult.Success);
            _innerSuccess = source;
        }
        
        [Benchmark(Description = "HandlePromiseSource - Pooled (1K ops)")]
        [BenchmarkCategory("Handle", "Small")]
        public void HandlePooled_Small()
        {
            for (int i = 0; i < SmallIterations; i++)
            {
                var source = HandlePromiseSource.Rent();
                source.Init(_innerSuccess);
                _ = source.GetResult();
                source.Dispose();
            }
        }
        
        [Benchmark(Description = "HandlePromiseSource - New (1K ops)")]
        [BenchmarkCategory("Handle", "Small")]
        public void HandleNew_Small()
        {
            for (int i = 0; i < SmallIterations; i++)
            {
                var source = new HandlePromiseSource();
                source.Init(_innerSuccess);
                _ = source.GetResult();
            }
        }
        
        [Benchmark(Description = "HandlePromiseSource - Pooled (10K ops)")]
        [BenchmarkCategory("Handle", "Medium")]
        public void HandlePooled_Medium()
        {
            for (int i = 0; i < MediumIterations; i++)
            {
                var source = HandlePromiseSource.Rent();
                source.Init(_innerSuccess);
                _ = source.GetResult();
                source.Dispose();
            }
        }
        
        [Benchmark(Description = "HandlePromiseSource - New (10K ops)")]
        [BenchmarkCategory("Handle", "Medium")]
        public void HandleNew_Medium()
        {
            for (int i = 0; i < MediumIterations; i++)
            {
                var source = new HandlePromiseSource();
                source.Init(_innerSuccess);
                _ = source.GetResult();
            }
        }
        
        [Benchmark(Description = "HandlePromiseSource - Pooled (100K ops)")]
        [BenchmarkCategory("Handle", "Large")]
        public void HandlePooled_Large()
        {
            for (int i = 0; i < LargeIterations; i++)
            {
                var source = HandlePromiseSource.Rent();
                source.Init(_innerSuccess);
                _ = source.GetResult();
                source.Dispose();
            }
        }
        
        [Benchmark(Description = "HandlePromiseSource - New (100K ops)")]
        [BenchmarkCategory("Handle", "Large")]
        public void HandleNew_Large()
        {
            for (int i = 0; i < LargeIterations; i++)
            {
                var source = new HandlePromiseSource();
                source.Init(_innerSuccess);
                _ = source.GetResult();
            }
        }
        
        #endregion
        
        #region HandleAsyncPromiseSource Benchmarks
        
        [Benchmark(Description = "HandleAsyncPromiseSource - Pooled (1K ops)")]
        [BenchmarkCategory("HandleAsync", "Small")]
        public void HandleAsyncPooled_Small()
        {
            for (int i = 0; i < SmallIterations; i++)
            {
                var source = HandleAsyncPromiseSource.Rent();
                source.Init(_innerSuccess);
                _ = source.GetResult();
                source.Dispose();
            }
        }
        
        [Benchmark(Description = "HandleAsyncPromiseSource - New (1K ops)")]
        [BenchmarkCategory("HandleAsync", "Small")]
        public void HandleAsyncNew_Small()
        {
            for (int i = 0; i < SmallIterations; i++)
            {
                var source = new HandleAsyncPromiseSource();
                source.Init(_innerSuccess);
                _ = source.GetResult();
            }
        }
        
        [Benchmark(Description = "HandleAsyncPromiseSource - Pooled (10K ops)")]
        [BenchmarkCategory("HandleAsync", "Medium")]
        public void HandleAsyncPooled_Medium()
        {
            for (int i = 0; i < MediumIterations; i++)
            {
                var source = HandleAsyncPromiseSource.Rent();
                source.Init(_innerSuccess);
                _ = source.GetResult();
                source.Dispose();
            }
        }
        
        [Benchmark(Description = "HandleAsyncPromiseSource - New (10K ops)")]
        [BenchmarkCategory("HandleAsync", "Medium")]
        public void HandleAsyncNew_Medium()
        {
            for (int i = 0; i < MediumIterations; i++)
            {
                var source = new HandleAsyncPromiseSource();
                source.Init(_innerSuccess);
                _ = source.GetResult();
            }
        }
        
        [Benchmark(Description = "HandleAsyncPromiseSource - Pooled (100K ops)")]
        [BenchmarkCategory("HandleAsync", "Large")]
        public void HandleAsyncPooled_Large()
        {
            for (int i = 0; i < LargeIterations; i++)
            {
                var source = HandleAsyncPromiseSource.Rent();
                source.Init(_innerSuccess);
                _ = source.GetResult();
                source.Dispose();
            }
        }
        
        [Benchmark(Description = "HandleAsyncPromiseSource - New (100K ops)")]
        [BenchmarkCategory("HandleAsync", "Large")]
        public void HandleAsyncNew_Large()
        {
            for (int i = 0; i < LargeIterations; i++)
            {
                var source = new HandleAsyncPromiseSource();
                source.Init(_innerSuccess);
                _ = source.GetResult();
            }
        }
        
        #endregion
        
        #region Combined Real-World Simulation
        
        [Benchmark(Description = "Real-world: TryHandle + Handle Pooled Chain")]
        [BenchmarkCategory("RealWorld")]
        public void RealWorldChain_Pooled()
        {
            for (int i = 0; i < SmallIterations; i++)
            {
                var tryHandleSource = TryHandlePromiseSource.Rent();
                tryHandleSource.Init(HandlingResult.Success);
                
                var handleSource = HandlePromiseSource.Rent();
                handleSource.Init(tryHandleSource);
                
                _ = handleSource.GetResult();
                
                handleSource.Dispose();
                tryHandleSource.Dispose();
            }
        }
        
        [Benchmark(Description = "Real-world: TryHandle + Handle New Chain")]
        [BenchmarkCategory("RealWorld")]
        public void RealWorldChain_New()
        {
            for (int i = 0; i < SmallIterations; i++)
            {
                var tryHandleSource = new TryHandlePromiseSource();
                tryHandleSource.Init(HandlingResult.Success);
                
                var handleSource = new HandlePromiseSource();
                handleSource.Init(tryHandleSource);
                
                _ = handleSource.GetResult();
            }
        }
        
        [Benchmark(Description = "Real-world: Async Chain Pooled")]
        [BenchmarkCategory("RealWorld")]
        public void RealWorldAsyncChain_Pooled()
        {
            var completedTask = Task.CompletedTask;
            for (int i = 0; i < SmallIterations; i++)
            {
                var tryHandleAsyncSource = TryHandleAsyncPromiseSource.Rent();
                tryHandleAsyncSource.Init(completedTask.GetAwaiter());
                
                var handleAsyncSource = HandleAsyncPromiseSource.Rent();
                handleAsyncSource.Init(tryHandleAsyncSource);
                
                _ = handleAsyncSource.GetResult();
                
                handleAsyncSource.Dispose();
                tryHandleAsyncSource.Dispose();
            }
        }
        
        [Benchmark(Description = "Real-world: Async Chain New")]
        [BenchmarkCategory("RealWorld")]
        public void RealWorldAsyncChain_New()
        {
            var completedTask = Task.CompletedTask;
            for (int i = 0; i < SmallIterations; i++)
            {
                var tryHandleAsyncSource = new TryHandleAsyncPromiseSource();
                tryHandleAsyncSource.Init(completedTask.GetAwaiter());
                
                var handleAsyncSource = new HandleAsyncPromiseSource();
                handleAsyncSource.Init(tryHandleAsyncSource);
                
                _ = handleAsyncSource.GetResult();
            }
        }
        
        #endregion
        
        #region Large Payload Simulations
        
        [Params(100, 1000, 10000)]
        public int VectorCount { get; set; }
        
        private Vector3[] _vectors = Array.Empty<Vector3>();
        
        [GlobalSetup]
        public void Setup()
        {
            _vectors = new Vector3[VectorCount];
            var random = new Random(42);
            for (int i = 0; i < VectorCount; i++)
            {
                _vectors[i] = new Vector3(
                    (float)random.NextDouble(),
                    (float)random.NextDouble(),
                    (float)random.NextDouble());
            }
        }
        
        [Benchmark(Description = "Large Payload - Pooled")]
        [BenchmarkCategory("LargePayload")]
        public float LargePayload_Pooled()
        {
            float sum = 0;
            for (int i = 0; i < 100; i++)
            {
                var source = TryHandlePromiseSource.Rent();
                source.Init(HandlingResult.Success);
                
                foreach (var vec in _vectors)
                    sum += vec.X + vec.Y + vec.Z;
                
                _ = source.GetResult();
                source.Dispose();
            }
            return sum;
        }
        
        [Benchmark(Description = "Large Payload - New")]
        [BenchmarkCategory("LargePayload")]
        public float LargePayload_New()
        {
            float sum = 0;
            for (int i = 0; i < 100; i++)
            {
                var source = new TryHandlePromiseSource();
                source.Init(HandlingResult.Success);
                
                foreach (var vec in _vectors)
                    sum += vec.X + vec.Y + vec.Z;
                
                _ = source.GetResult();
            }
            return sum;
        }
        
        #endregion
    }
}
