namespace Chopsticks.Messages.Registration
{
    /// <summary>
    /// Interface for registrations that are ordered by priority and registration sequence.
    /// </summary>
    public interface IOrderedRegistration
    {
        /// <summary>
        /// The sort priority (lower = higher priority).
        /// </summary>
        int Order { get; }
        
        /// <summary>
        /// The registration sequence number for stable ordering among equal priorities.
        /// </summary>
        int RegistrationIndex { get; }
    }
}
