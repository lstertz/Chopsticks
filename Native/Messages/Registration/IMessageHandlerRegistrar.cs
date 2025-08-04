using Chopsticks.Messages.Handlers;
using Chopsticks.Messages.Interception;

namespace Chopsticks.Messages.Registration
{
    public interface IMessageHandlerRegistrar<TMessage>
    {
        // TODO :: Accommodate clearing all.
        bool Register(IMessageHandler<TMessage> handler, 
            RegistrationSettings settings, params IIntercept<TMessage>[] interceptors);


        bool Register(IMessageHandler<TMessage> handler,
            params IIntercept<TMessage>[] interceptors) =>
                Register(handler, default, interceptors);

        void Unregister(IMessageHandler<TMessage> handler);
    }
}
