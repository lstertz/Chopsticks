namespace Chopsticks.Messages.Registration;

public interface IHandlerRegistrar<THandler, TInterceptor>
{
    // TODO :: Accommodate clearing all.
    bool Register(THandler handler,
        RegistrationSettings settings, params TInterceptor[] interceptors);


    bool Register(THandler handler,
        params TInterceptor[] interceptors) =>
            Register(handler, default, interceptors);

    void Unregister(THandler handler);
}
