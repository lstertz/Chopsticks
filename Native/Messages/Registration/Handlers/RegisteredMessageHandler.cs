using Chopsticks.Messages.Handlers;
using System;

namespace Chopsticks.Messages.Registration.Handlers;

public class RegisteredMessageHandler<TMessage, TContext> : 
    BaseRegisteredHandler<TMessage, TContext>,
    IEquatable<RegisteredMessageHandler<TMessage, TContext>>
    where TContext : IMessageContext<TMessage>, new()
{
    private readonly IMessageHandler<TMessage> _handler;

    // TODO :: Support wrapping in multicast per-handler interceptors.
    public RegisteredMessageHandler(IMessageHandler<TMessage> handler) => 
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));


    /// <inheritdoc/>
    protected override HandlingResultAwaitable InternalHandleAsync(TContext context) =>
        _handler.TryHandleAsync(context.Message, context.CancellationToken);


    /// <inheritdoc/>
    public bool Equals(RegisteredMessageHandler<TMessage, TContext> other) =>
        _handler.Equals(other._handler);

    /// <inheritdoc/>
    public override int GetHashCode() =>
        _handler.GetHashCode();
}
