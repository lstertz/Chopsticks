using System;
using System.Threading.Tasks;

namespace Chopsticks.Messages
{
    public static class TaskResultContinuations
    {
        // TODO :: Support other continuations.

        public static async Task<HandlingResult> WhenCancelledAsync(
            this Task<HandlingResult> awaitable,
            Func<Task> whenCancelled)
        {
            var result = await awaitable;
            if (result.Status == HandlingStatus.Cancelled)
                await whenCancelled();

            return result;
        }

        public static async Task<HandlingResult> WhenSuccessfulAsync(
            this Task<HandlingResult> awaitable,
            Func<Task> whenSuccessful)
        {
            var result = await awaitable;
            if (result.Status == HandlingStatus.Success)
                await whenSuccessful();

            return result;
        }
    }
}
