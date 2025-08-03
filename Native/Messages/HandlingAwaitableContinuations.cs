using System.Threading.Tasks;

namespace Chopsticks.Messages
{
    public static class HandlingAwaitableContinuations
    {
        public static async Task<HandlingResult> ContinueWithTask(
            this HandlingAwaitable awaitable) => 
            await awaitable;
        public static async ValueTask<HandlingResult> ContinueWithValueTask(
            this HandlingAwaitable awaitable) => 
            await awaitable;

        // TODO :: Support other async types.
    }
}
