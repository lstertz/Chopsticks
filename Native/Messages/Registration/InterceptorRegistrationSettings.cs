namespace Chopsticks.Messages.Registration;

/// <summary>
/// Settings to define how an interceptor registered through a 
/// <see cref="IMessageHandlerRegistrar{TMessage}"/>
/// will be processed.
/// </summary>
public readonly struct InterceptorRegistrationSettings
{
    /// <summary>
    /// The relative order that this interceptor will be executed by any collective handler 
    /// system, such as a multicast handler, relative to other interceptors registered 
    /// in the same fashion (e.g., on dispatch vs. per handler).
    /// </summary>
    /// <remarks>
    /// 0 is the default; more negative numbers will process earlier and more 
    /// positive numbers will process later.</remarks>
    public int Order { get; init; }
}
