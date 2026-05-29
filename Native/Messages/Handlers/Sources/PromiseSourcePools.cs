using Chopsticks.Messages.Registration;
using System.Threading;

namespace Chopsticks.Messages.Handlers.Sources
{
    /// <summary>
    /// Provides centralized pool management for promise sources.
    /// Call <see cref="PreWarm"/> at application startup for optimal performance.
    /// </summary>
    public static class PromiseSourcePools
    {
        private const int DefaultPreWarmCount = 16;
        
        private static int _isPreWarmed;
        
        /// <summary>
        /// Gets whether the pools have been pre-warmed.
        /// </summary>
        public static bool IsPreWarmed => Volatile.Read(ref _isPreWarmed) == 1;
        
        /// <summary>
        /// Pre-warms all promise source pools by creating initial instances.
        /// Call this at application startup for optimal performance.
        /// </summary>
        /// <param name="countPerPool">Number of instances to create per pool. Default is 16.</param>
        public static void PreWarm(int countPerPool = DefaultPreWarmCount)
        {
            // Thread-safe one-time initialization
            if (Interlocked.CompareExchange(ref _isPreWarmed, 1, 0) != 0)
                return;
                
            PreWarmTryHandlePromiseSources(countPerPool);
            PreWarmTryHandleAsyncPromiseSources(countPerPool);
            PreWarmHandlePromiseSources(countPerPool);
            PreWarmHandleAsyncPromiseSources(countPerPool);
        }
        
        /// <summary>
        /// Pre-warms the SequentialHandlingPromiseSource pool for a specific message type.
        /// Call this for your hot message types at startup for optimal multicast performance.
        /// </summary>
        /// <typeparam name="TMessage">The message type.</typeparam>
        /// <param name="count">Number of instances to create. Default is 16.</param>
        public static void PreWarmSequentialSource<TMessage>(int count = DefaultPreWarmCount)
        {
            PreWarmSequentialSourceInternal<TMessage, DefaultMessageContext<TMessage>>(count);
        }
        
        /// <summary>
        /// Pre-warms the SequentialHandlingPromiseSource pool for a specific message and context type.
        /// Call this for your hot message types at startup for optimal multicast performance.
        /// </summary>
        /// <typeparam name="TMessage">The message type.</typeparam>
        /// <typeparam name="TContext">The context type.</typeparam>
        /// <param name="count">Number of instances to create. Default is 16.</param>
        public static void PreWarmSequentialSource<TMessage, TContext>(int count = DefaultPreWarmCount)
            where TContext : IMessageContext<TMessage>, new()
        {
            PreWarmSequentialSourceInternal<TMessage, TContext>(count);
        }
        
        private static void PreWarmSequentialSourceInternal<TMessage, TContext>(int count)
            where TContext : IMessageContext<TMessage>, new()
        {
            var sources = new SequentialHandlingPromiseSource<TMessage, TContext>[count];
            for (int i = 0; i < count; i++)
            {
                sources[i] = SequentialHandlingPromiseSource<TMessage, TContext>.Rent();
            }
            for (int i = 0; i < count; i++)
            {
                sources[i].Dispose();
            }
        }
        
        private static void PreWarmTryHandlePromiseSources(int count)
        {
            var sources = new TryHandlePromiseSource[count];
            for (int i = 0; i < count; i++)
            {
                sources[i] = TryHandlePromiseSource.Rent();
            }
            for (int i = 0; i < count; i++)
            {
                sources[i].Dispose();
            }
        }
        
        private static void PreWarmTryHandleAsyncPromiseSources(int count)
        {
            var sources = new TryHandleAsyncPromiseSource[count];
            for (int i = 0; i < count; i++)
            {
                sources[i] = TryHandleAsyncPromiseSource.Rent();
            }
            for (int i = 0; i < count; i++)
            {
                sources[i].Dispose();
            }
        }
        
        private static void PreWarmHandlePromiseSources(int count)
        {
            var sources = new HandlePromiseSource[count];
            for (int i = 0; i < count; i++)
            {
                sources[i] = HandlePromiseSource.Rent();
            }
            for (int i = 0; i < count; i++)
            {
                sources[i].Dispose();
            }
        }
        
        private static void PreWarmHandleAsyncPromiseSources(int count)
        {
            var sources = new HandleAsyncPromiseSource[count];
            for (int i = 0; i < count; i++)
            {
                sources[i] = HandleAsyncPromiseSource.Rent();
            }
            for (int i = 0; i < count; i++)
            {
                sources[i].Dispose();
            }
        }
    }
}
