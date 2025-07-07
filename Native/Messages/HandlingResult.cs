namespace Chopsticks.Messages
{
    public readonly struct HandlingResult
    {
        public static HandlingResult NoHandlers { get; } = new HandlingResult
        {
            Status = HandlingStatus.Unprocessed
        };

        public static HandlingResult Success { get; } = new HandlingResult
        {
            Status = HandlingStatus.Success
        };

        public HandlingStatus Status { get; init; }


        // TODO :: Support wrapping exceptions for failed results.
    }
}
