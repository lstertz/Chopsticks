namespace Chopsticks.Messages
{
    /// <summary>
    /// Settings to define how a receiver registered with a 
    /// <see cref="Abstractions.ICollectiveMessageReceiver{TMessage, TAsync}"/> 
    /// will be processed.
    /// </summary>
    public struct RegistrationSettings
    {
        /// <summary>
        /// The relative order that this receiver will be handled by a 
        /// <see cref="Abstractions.ICollectiveMessageReceiver{TMessage, TAsync}"/> 
        /// when message processing is to be done in sequence.
        /// </summary>
        /// <remarks>
        /// 0 is the default; more negative numbers will process earlier and more 
        /// positive numbers will process later.</remarks>
        public int Order { get; init; }
    }
}
