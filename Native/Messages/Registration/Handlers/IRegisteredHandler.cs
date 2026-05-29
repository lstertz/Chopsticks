using Chopsticks.Messages.Handlers;
using Chopsticks.Messages.Interceptors;
using Chopsticks.Messages.Registration.Interceptors;

namespace Chopsticks.Messages.Registration.Handlers;


public interface IRegisteredHandler
{
}

public interface IRegisteredHandler<TMessage> :
    IRegisteredHandler
{
    /// <summary>
    /// Adds a generic interceptor to this handler's pipeline.
    /// </summary>
    /// <param name="interceptor">The interceptor to add.</param>
    /// <param name="settings">Optional registration settings.</param>
    /// <returns>The interceptor registration, which can be used to remove the interceptor later.</returns>
    IRegisteredInterceptor AddInterceptor(
        IInterceptor interceptor,
        InterceptorRegistrationSettings settings = default);

    /// <summary>
    /// Adds a message-specific interceptor to this handler's pipeline.
    /// </summary>
    /// <param name="interceptor">The interceptor to add.</param>
    /// <param name="settings">Optional registration settings.</param>
    /// <returns>The interceptor registration, which can be used to remove the interceptor later.</returns>
    IRegisteredInterceptor AddInterceptor(
        IMessageInterceptor<TMessage> interceptor,
        InterceptorRegistrationSettings settings = default);

    /// <summary>
    /// Adds a contract interceptor to this handler's pipeline.
    /// </summary>
    /// <typeparam name="TContract">The contract type.</typeparam>
    /// <param name="interceptor">The interceptor to add.</param>
    /// <param name="mode">The contract interception mode.</param>
    /// <param name="settings">Optional registration settings.</param>
    /// <returns>The interceptor registration, which can be used to remove the interceptor later.</returns>
    IRegisteredInterceptor AddInterceptor<TContract>(
        IContractInterceptor<TContract> interceptor,
        ContractInterceptorMode mode = ContractInterceptorMode.Required,
        InterceptorRegistrationSettings settings = default);
    
    /// <summary>
    /// Removes a previously registered interceptor from this handler.
    /// </summary>
    /// <param name="registration">The interceptor registration returned from AddInterceptor.</param>
    /// <returns>True if the interceptor was found and removed, false otherwise.</returns>
    bool RemoveInterceptor(IRegisteredInterceptor registration);
}

public interface IRegisteredHandler<TMessage, TContext> :
    IRegisteredHandler<TMessage>
    where TContext : IMessageContext<TMessage>, new()
{
    /// <summary>
    /// Adds a generic interceptor to this handler's pipeline.
    /// </summary>
    new IRegisteredInterceptor AddInterceptor(
        IInterceptor interceptor,
        InterceptorRegistrationSettings settings = default);

    /// <summary>
    /// Adds a message-specific interceptor to this handler's pipeline.
    /// </summary>
    new IRegisteredInterceptor AddInterceptor(
        IMessageInterceptor<TMessage> interceptor,
        InterceptorRegistrationSettings settings = default);

    /// <summary>
    /// Adds a contract interceptor to this handler's pipeline.
    /// </summary>
    new IRegisteredInterceptor AddInterceptor<TContract>(
        IContractInterceptor<TContract> interceptor,
        ContractInterceptorMode mode = ContractInterceptorMode.Required,
        InterceptorRegistrationSettings settings = default);

    /// <summary>
    /// Adds a context-specific interceptor to this handler's pipeline.
    /// </summary>
    /// <param name="interceptor">The interceptor to add.</param>
    /// <param name="settings">Optional registration settings.</param>
    /// <returns>The interceptor registration, which can be used to remove the interceptor later.</returns>
    IRegisteredInterceptor AddInterceptor(
        IContextInterceptor<TMessage, TContext> interceptor,
        InterceptorRegistrationSettings settings = default);
}
