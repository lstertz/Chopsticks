namespace Chopsticks.Messages.Registration;

/// <summary>
/// Settings to define how a handler registered to a 
/// <see cref="Handlers.Multicast.IMulticastMessageHandler{TMessage}"/> through a 
/// <see cref="IMessageHandlerRegistrar{TMessage}"/>
/// will be processed.
/// </summary>
public readonly struct RegistrationSettings
{
    /// <summary>
    /// The relative order that this handler will be handled by a 
    /// <see cref="Handlers.Multicast.IMulticastMessageHandler{TMessage}"/> 
    /// when message processing is to be done in sequence.
    /// </summary>
    /// <remarks>
    /// 0 is the default; more negative numbers will process earlier and more 
    /// positive numbers will process later.</remarks>
    public int Order { get; init; }
}
