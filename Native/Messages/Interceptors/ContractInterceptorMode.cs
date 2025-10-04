namespace Chopsticks.Messages.Interceptors
{
    // TODO :: See if shared and contract can be merged (if they should be merged).
    // TODO :: Refactor interceptor adding to be done on a handler registration 
    //             that is returned from registering a handler.
    //             Registrations will need knowledge of their registrar to get 
    //             the other interceptors to rebuild pipelines.
    // TODO :: Refactor interceptor internals to work from IBaseInterceptor.


    public enum ContractInterceptorMode
    {
        Required,
        Optional
    }
}
