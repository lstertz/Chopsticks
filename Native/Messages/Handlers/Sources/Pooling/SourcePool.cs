using System.Collections.Concurrent;

namespace Chopsticks.Messages.Handlers.Sources.Pooling
{
    // TODO :: Determine where the pool instance is held.
    //        Possibly a static instance on each source implementation.
    //        Needs to be accessible to the awaiters that hold the source reference.

    /// <summary>
    /// A pool of <see cref="IHandlingPromiseSource"/> objects.
    /// </summary>
    /// <typeparam name="TSource">The type of the source to pool.</typeparam>
    public class SourcePool<TSource>
        where TSource : class, IHandlingPromiseSource, new()
    {
        private readonly ConcurrentBag<TSource> _available =
            [
                new(),
                new(),
                new(),
                new()
            ];
        private readonly ConcurrentDictionary<TSource, TSource> _rented = new();

        /// <inheritdoc/>
        public TSource Rent()
        {
            if (!_available.TryTake(out TSource source))
                source = new();  // All available sources are already rented.

            _rented.TryAdd(source, source);
            return source;
        }

        /// <inheritdoc/>
        public void Return(TSource source)
        {
            if (!_rented.TryRemove(source, out var rentedSource))
                return;  // This source wasn't rented.

            rentedSource.Reset();
            _available.Add(rentedSource);
        }
    }
}