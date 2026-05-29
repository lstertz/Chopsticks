using Chopsticks.Dependencies.Containers;
using System;
using System.Collections.Concurrent;

namespace Chopsticks.Dependencies.Resolutions
{
    /// <inheritdoc/>
    /// <remarks>
    /// This resolution assumes the dependency has a lifetime 
    /// of <see cref="DependencyLifetime.Contained"/>.
    /// Thread-safe: uses ConcurrentDictionary for safe concurrent access
    /// across multiple containers.
    /// </remarks>
    public class ContainedResolution(Type contract,
        Func<IDependencyContainer, object?> factory) :
        DependencyResolution(contract, factory)
    {
        private readonly ConcurrentDictionary<IDependencyContainer, Lazy<object?>> _instances = new();


        /// <inheritdoc/>
        public override void Dispose()
        {
            base.Dispose();

            foreach (var lazy in _instances.Values)
                if (lazy.IsValueCreated && lazy.Value is IDisposable disposable)
                    disposable.Dispose();

            _instances.Clear();
        }

        /// <inheritdoc/>
        public override void DisposeFor(IDependencyContainer container)
        {
            if (_instances.TryRemove(container, out var lazy))
                if (lazy.IsValueCreated && lazy.Value is IDisposable disposable)
                    disposable.Dispose();
        }


        /// <inheritdoc/>
        public override object? Get(IDependencyContainer container)
        {
            if (Factory == null)
                return null;

            var lazy = _instances.GetOrAdd(container, 
                c => new Lazy<object?>(() => Factory!.Invoke(c)));
            return lazy.Value;
        }
    }
}
