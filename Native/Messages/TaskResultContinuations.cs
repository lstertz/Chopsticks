using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Chopsticks.Messages
{
    public static class TaskResultContinuations
    {
        public static async Task<HandlingResult> ContinueWithTask(
            this HandlingResultAwaitable awaitable) =>
            await awaitable;


        public static async Task<HandlingResult> ThrowIfFailed(
            this Task<HandlingResult> awaitable)
        {
            var result = await awaitable;
            result.ThrowIfFailed();

            return result;
        }

        public static async Task<HandlingResult> ThrowIfNotHandled(
            this Task<HandlingResult> awaitable)
        {
            var result = await awaitable;
            result.ThrowIfNotHandled();

            return result;
        }


        public static async Task<HandlingResult> WhenCancelledAsync(
            this Task<HandlingResult> awaitable,
            Func<Task> whenCancelled)
        {
            var result = await awaitable;
            if (result.Status == HandlingStatus.Cancelled)
                await whenCancelled();

            return result;
        }

        public static async Task<HandlingResult> WhenCompletedAsync(
            this Task<HandlingResult> awaitable,
            Func<Task> whenCompleted)
        {
            var result = await awaitable;
            if ((result.Status & HandlingStatus.Completed) != 0)
                await whenCompleted();

            return result;
        }

        public static async Task<HandlingResult> WhenFailedAsync(
            this Task<HandlingResult> awaitable,
            Func<IEnumerable<Exception>, Task> whenFailed)
        {
            var result = await awaitable;
            if (result.Status == HandlingStatus.Failure)
                await whenFailed(result.Exceptions);

            return result;
        }

        public static async Task<HandlingResult> WhenNotHandledAsync(
            this Task<HandlingResult> awaitable,
            Func<Task> whenNotHandled)
        {
            var result = await awaitable;
            if (result.Status == HandlingStatus.NotHandled)
                await whenNotHandled();

            return result;
        }

        public static async Task<HandlingResult> WhenNotSuccessfulAsync(
            this Task<HandlingResult> awaitable,
            Func<Task> whenNotSuccessful)
        {
            var result = await awaitable;
            if ((result.Status & HandlingStatus.NonSuccess) != 0)
                await whenNotSuccessful();

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
