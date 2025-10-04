using Chopsticks.Messages.Handlers;
using Chopsticks.Messages.Interceptors;

namespace Chopsticks.Messages.Registration.Handlers;


public interface IRegisteredHandler
{
}

public interface IRegisteredHandler<TMessage> :
    IRegisteredHandler
{
    IRegisteredHandler<TMessage> AddInterceptor(
        IInterceptor interceptor,
        InterceptorRegistrationSettings settings = default);

    IRegisteredHandler<TMessage> AddInterceptor(
        IMessageInterceptor<TMessage> interceptor,
        InterceptorRegistrationSettings settings = default);

    IRegisteredHandler<TMessage> AddInterceptor<TContract>(
        IContractInterceptor<TContract> interceptor,
        ContractInterceptorMode mode = ContractInterceptorMode.Required,
        InterceptorRegistrationSettings settings = default);
}

public interface IRegisteredHandler<TMessage, TContext> :
    IRegisteredHandler<TMessage>
    where TContext : IMessageContext<TMessage>, new()
{
    new IRegisteredHandler<TMessage, TContext> AddInterceptor(
        IInterceptor interceptor,
        InterceptorRegistrationSettings settings = default);

    new IRegisteredHandler<TMessage, TContext> AddInterceptor(
        IMessageInterceptor<TMessage> interceptor,
        InterceptorRegistrationSettings settings = default);

    new IRegisteredHandler<TMessage, TContext> AddInterceptor<TContract>(
        IContractInterceptor<TContract> interceptor,
        ContractInterceptorMode mode = ContractInterceptorMode.Required,
        InterceptorRegistrationSettings settings = default);

    IRegisteredHandler<TMessage, TContext> AddInterceptor(
        IContextInterceptor<TMessage, TContext> interceptor,
        InterceptorRegistrationSettings settings = default);
}
