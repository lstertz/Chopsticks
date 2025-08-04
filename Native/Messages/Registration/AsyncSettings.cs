namespace Chopsticks.Messages
{
    public class AsyncSettings
    {
        /// <summary>
        /// Specifies whether handlers should be run in parallel or sequentially.
        /// </summary>
        public bool RunParallel { get; set; }

        /// <summary>
        /// Specifies whether non-async handlers should be captured within 
        /// async runners and executed asynchronously.
        /// </summary>
        /// <remarks>
        /// If false, then non-async handlers will run synchronously while 
        /// being represented by an async return value.</remarks>
        public bool RunNonAsyncAsAsync { get; set; }
    }
}
