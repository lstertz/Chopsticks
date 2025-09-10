using Chopsticks.Messages.Handlers;
namespace Chopsticks.Messages.Registration;

public interface IMessageHandlerRegistrar<TMessage>
{
    // TODO :: Accommodate clearing all.

    bool Register(IMessageHandler<TMessage> handler, HandlerRegistrationSettings settings);


    bool Register(IMessageHandler<TMessage> handler) =>
        Register(handler, default);

    void Unregister(IMessageHandler<TMessage> handler);
}
