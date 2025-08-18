using Chopsticks.Messages.Handlers;
using System;

namespace Chopsticks.Messages.Registration.Handlers;

public class RegisteredContextHandler<TMessage, TContext> : 
    BaseRegisteredHandler<TMessage, TContext>,
    IEquatable<RegisteredContextHandler<TMessage, TContext>>
    where TContext : IMessageContext<TMessage>, new()
{
    private readonly IContextHandler<TMessage, TContext> _handler;

    // TODO :: Support wrapping in multicast per-handler interceptors.
    public RegisteredContextHandler(IContextHandler<TMessage, TContext> handler) =>
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));


    public override HandlingAwaitable HandleAsync(TContext context) =>
        _handler.HandleAsync(context);


    /// <inheritdoc/>
    public bool Equals(RegisteredContextHandler<TMessage, TContext> other) =>
        _handler.Equals(other._handler);

    /// <inheritdoc/>
    public override int GetHashCode() =>
        _handler.GetHashCode();
}
