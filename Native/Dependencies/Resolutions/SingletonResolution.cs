using Chopsticks.Dependencies.Containers;
using System;

namespace Chopsticks.Dependencies.Resolutions
{
    /// <inheritdoc/>
    /// <remarks>
    /// This resolution assumes the dependency has a lifetime 
    /// of <see cref="DependencyLifetime.Singleton"/>.
    /// Thread-safe: uses double-checked locking to ensure the factory
    /// is invoked at most once, even under concurrent access.
    /// </remarks>
    public class SingletonResolution(Type contract,
        Func<IDependencyContainer, object?> factory) :
        DependencyResolution(contract, factory)
    {
        private object? _instance;
        private readonly object _lock = new();
        private volatile bool _isCreated;

        /// <inheritdoc/>
        public override void Dispose()
        {
            base.Dispose();

            lock (_lock)
            {
                if (_instance is IDisposable disposable)
                    disposable.Dispose();

                _instance = null;
                _isCreated = false;
            }
        }

        /// <inheritdoc/>
        public override object? Get(IDependencyContainer container)
        {
            if (_isCreated)
                return _instance;

            lock (_lock)
            {
                if (_isCreated)
                    return _instance;

                _instance = Factory?.Invoke(container);
                _isCreated = true;
                return _instance;
            }
        }
    }
}
