namespace Chopsticks.Messages.Handlers.Sources.Pooling
{
    /// <summary>
    /// Defines a pool of <see cref="IHandlingPromiseSource"/> objects.
    /// </summary>
    public interface ISourcePool
    {
        /// <summary>
        /// Rents a source from the pool.
        /// </summary>
        /// <returns>The rented source.</returns>
        IHandlingPromiseSource Rent();

        /// <summary>
        /// Returns a source to the pool.
        /// </summary>
        /// <param name="source">The source to return.</param>
        void Return(IHandlingPromiseSource source);
    }
}