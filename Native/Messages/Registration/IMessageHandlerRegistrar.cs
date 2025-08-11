using Chopsticks.Messages.Handlers;
namespace Chopsticks.Messages.Registration
{
    public interface IMessageHandlerRegistrar<THandler, TInterceptor>
    {
        // TODO :: Accommodate clearing all.
        bool Register(THandler handler, 
            RegistrationSettings settings, params TInterceptor[] interceptors);


        bool Register(THandler handler,
            params TInterceptor[] interceptors) =>
                Register(handler, default, interceptors);

        void Unregister(THandler handler);
    }
}
