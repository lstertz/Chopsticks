namespace Chopsticks.Messages
{
    public readonly struct MessageResult
    {
        public static MessageResult NoReceivers { get; } = new MessageResult
        {
            CurrentStatus = Status.Unprocessed
        };

        public static MessageResult Success { get; } = new MessageResult
        {
            CurrentStatus = Status.Success
        };


        public enum Status
        {
            Success,
            Failure,
            Processing,
            Unprocessed
        }

        public Status CurrentStatus { get; init; }


        // TODO :: Support wrapping exceptions for failed results.
    }
}
