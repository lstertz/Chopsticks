namespace Chopsticks.Messages.Handlers.Sources.Pooling
{
    /// <summary>
    /// Defines the required functionality for a source to be pooled.
    /// </summary>
    public interface IPooledSource
    {
        /// <summary>
        /// Resets the pooled source so it can be safely used again.
        /// </summary>
        void Reset();
    }
}