using Chopsticks.Messages.Handlers;
using Chopsticks.Messages.Interceptors;
using System;

namespace Chopsticks.Messages.Registration.Interceptors;

public class RegisteredInterceptor<TMessage, TContext> : 
    IEquatable<RegisteredInterceptor<TMessage, TContext>>, 
    IRegisteredInterceptor,
    IOrderedRegistration
    where TContext : IMessageContext<TMessage>, new()
{
    public int Order { get; init; } = 0;

    public int RegistrationIndex { get; init; } = 0;

    private readonly IContextInterceptor<TMessage, TContext> _interceptor;

    public RegisteredInterceptor(IContextInterceptor<TMessage, TContext> interceptor) =>
        _interceptor = interceptor ?? throw new ArgumentNullException(nameof(interceptor));


    public HandlingResultAwaitable InterceptAsync(TContext context, 
        Func<TContext, HandlingResultAwaitable> next) =>
        _interceptor.InterceptAsync(context, next);


    /// <inheritdoc/>
    public bool Equals(RegisteredInterceptor<TMessage, TContext> other) =>
        _interceptor.Equals(other._interceptor);

    /// <inheritdoc/>
    public override int GetHashCode() =>
        _interceptor.GetHashCode();
}
