using Chopsticks.Messages.Handlers.Sources;
using Chopsticks.Messages.Registration;
using Chopsticks.Messages.Registration.Handlers;
using Chopsticks.Messages.Registration.Interceptors;
using System;
using System.Collections.Generic;
using System.Threading;

namespace Chopsticks.Messages.Handlers.Multicast;

public abstract class BaseMulticastHandler<TMessage, TContext> :
    BaseMessageHandlerRegistrar<TMessage, TContext>
    where TContext : IMessageContext<TMessage>, new()
{
    // TODO :: Verify thread safety.
    protected volatile Func<TContext, HandlingResultAwaitable> _dispatchPipeline = default!;
    // TODO :: Support stopping at the first failure.

    protected override void RebuildDispatchPipeline(
        List<RegisteredInterceptor<TMessage, TContext>> interceptors)
    {
        // OPTIMIZATION: Pass context directly through pipeline instead of reconstructing
        Func<TContext, HandlingResultAwaitable> current = InitiateDispatch;

        for (int c = interceptors.Count - 1; c >= 0; c--)
        {
            var next = current;
            var interceptor = interceptors[c];
            current = (context) =>
                interceptor.InterceptAsync(context, next);
        }

        _dispatchPipeline = current;
    }

    /// <summary>
    /// Initiates dispatch with sync fast-path optimization.
    /// When all handlers complete synchronously, avoids promise source allocation entirely.
    /// </summary>
    private HandlingResultAwaitable InitiateDispatch(TContext context)
    {
        var handlers = RegisteredMessageHandlers;
        
        // No handlers - return immediately without allocation
        if (handlers.Length == 0)
            return new HandlingResultAwaitable(HandlingResult.NoHandlers);
        
        // Try sync fast-path: execute handlers inline until one is async
        return TryExecuteSyncFastPath(handlers, context);
    }
    
    /// <summary>
    /// Original dispatch path - always uses SequentialHandlingPromiseSource.
    /// Used for comparison/debugging.
    /// </summary>
    private HandlingResultAwaitable InitiateDispatchOriginal(TContext context)
    {
        var source = SequentialHandlingPromiseSource<TMessage, TContext>.Rent();
        source.Init(RegisteredMessageHandlers);
        source.Run(context);
        return new HandlingResultAwaitable(source);
    }
    
    /// <summary>
    /// Attempts to execute all handlers synchronously without allocating a promise source.
    /// Falls back to SequentialHandlingPromiseSource if any handler is async.
    /// </summary>
    private HandlingResultAwaitable TryExecuteSyncFastPath(
        BaseRegisteredHandler<TMessage, TContext>[] handlers, 
        TContext context)
    {
        var aggregateStatus = HandlingStatus.NotHandled;
        List<Exception>? exceptions = null;
        
        for (int i = 0; i < handlers.Length; i++)
        {
            var awaitable = handlers[i].HandleAsync(context);
            var awaiter = awaitable.GetAwaiter();
            
            // If any handler is async, fall back to full promise source path
            if (!awaiter.IsCompleted)
            {
                return FallbackToPromiseSource(handlers, context, i, aggregateStatus, exceptions, awaiter);
            }
            
            // Sync handler - merge result immediately
            try
            {
                var result = awaiter.GetResult();
                MergeHandlerResult(result, ref aggregateStatus, ref exceptions);
            }
            catch (Exception ex)
            {
                exceptions ??= new List<Exception>(4);
                exceptions.Add(ex);
                aggregateStatus = HandlingStatus.Failure;
            }
        }
        
        // All handlers completed synchronously - return direct result (zero allocation)
        return new HandlingResultAwaitable(BuildAggregateResult(aggregateStatus, exceptions));
    }
    
    /// <summary>
    /// Falls back to SequentialHandlingPromiseSource when async handler encountered.
    /// Continues from the current index with already-accumulated state.
    /// </summary>
    private HandlingResultAwaitable FallbackToPromiseSource(
        BaseRegisteredHandler<TMessage, TContext>[] handlers,
        TContext context,
        int asyncHandlerIndex,
        HandlingStatus currentStatus,
        List<Exception>? exceptions,
        HandlingResultAwaitable.Awaiter pendingAwaiter)
    {
        var source = SequentialHandlingPromiseSource<TMessage, TContext>.Rent();
        source.Init(handlers);
        source.RunFromIndex(context, asyncHandlerIndex, currentStatus, exceptions, pendingAwaiter);
        return new HandlingResultAwaitable(source);
    }
    
    /// <summary>
    /// Merges a handler's result into the aggregate status.
    /// </summary>
    private static void MergeHandlerResult(
        HandlingResult result, 
        ref HandlingStatus aggregateStatus, 
        ref List<Exception>? exceptions)
    {
        if (result.Status == HandlingStatus.NotHandled)
            return;
        
        if (result.Status == HandlingStatus.Failure)
        {
            exceptions ??= new List<Exception>(4);
            result.AddExceptionsTo(exceptions);
            aggregateStatus = HandlingStatus.Failure;
            return;
        }
        
        if (aggregateStatus == HandlingStatus.Failure)
            return;
        
        if (result.Status == HandlingStatus.Cancelled)
        {
            aggregateStatus = HandlingStatus.Cancelled;
            return;
        }
        
        if (aggregateStatus == HandlingStatus.NotHandled || 
            aggregateStatus == HandlingStatus.Success)
        {
            aggregateStatus = result.Status;
        }
    }
    
    /// <summary>
    /// Builds the final aggregate result from accumulated state.
    /// </summary>
    private static HandlingResult BuildAggregateResult(
        HandlingStatus status, 
        List<Exception>? exceptions)
    {
        if (exceptions is { Count: > 0 })
            return HandlingResult.FromExceptions(exceptions);
        
        return status switch
        {
            HandlingStatus.Success => HandlingResult.Success,
            HandlingStatus.Cancelled => HandlingResult.Cancelled,
            _ => HandlingResult.NoHandlers
        };
    }
    
    /// <summary>
    /// Dispatches the message to all handlers without creating a promise source.
    /// Zero allocation fire-and-forget pattern. Exceptions are swallowed.
    /// </summary>
    /// <remarks>
    /// Use this for fire-and-forget scenarios where you don't need the result.
    /// For scenarios where you need error handling, use <see cref="HandleSync"/> instead.
    /// </remarks>
    protected void DispatchFireAndForget(TMessage message, CancellationToken token = default)
    {
        var handlers = RegisteredMessageHandlers;
        if (handlers.Length == 0)
            return;
        
        var context = new TContext
        {
            CancellationToken = token,
            Message = message
        };
        
        for (int i = 0; i < handlers.Length; i++)
        {
            try
            {
                var awaitable = handlers[i].HandleAsync(context);
                if (!awaitable.GetAwaiter().IsCompleted)
                {
                    // For async handlers, we can't truly fire-and-forget without allocation
                    // Just get the result to trigger completion (may block briefly)
                    awaitable.GetAwaiter().GetResult();
                }
                else
                {
                    // Sync handler - just consume the result
                    _ = awaitable.GetAwaiter().GetResult();
                }
            }
            catch
            {
                // Fire-and-forget swallows exceptions
            }
        }
    }
    
    /// <summary>
    /// Dispatches the message to all handlers synchronously and returns the aggregated result.
    /// Zero allocation when all handlers are synchronous.
    /// </summary>
    /// <returns>The aggregated handling result from all handlers.</returns>
    protected HandlingResult DispatchSync(TMessage message, CancellationToken token = default)
    {
        var handlers = RegisteredMessageHandlers;
        if (handlers.Length == 0)
            return HandlingResult.NoHandlers;
        
        var context = new TContext
        {
            CancellationToken = token,
            Message = message
        };
        
        var aggregateStatus = HandlingStatus.NotHandled;
        List<Exception>? exceptions = null;
        
        for (int i = 0; i < handlers.Length; i++)
        {
            try
            {
                var awaitable = handlers[i].HandleAsync(context);
                var result = awaitable.GetAwaiter().GetResult();
                
                // Merge result status
                if (result.Status == HandlingStatus.Failure)
                {
                    exceptions ??= new List<Exception>(4);
                    result.AddExceptionsTo(exceptions);
                    aggregateStatus = HandlingStatus.Failure;
                }
                else if (aggregateStatus != HandlingStatus.Failure)
                {
                    if (result.Status == HandlingStatus.Cancelled)
                        aggregateStatus = HandlingStatus.Cancelled;
                    else if (result.Status == HandlingStatus.Success)
                        aggregateStatus = HandlingStatus.Success;
                }
            }
            catch (Exception ex)
            {
                exceptions ??= new List<Exception>(4);
                exceptions.Add(ex);
                aggregateStatus = HandlingStatus.Failure;
            }
        }
        
        if (exceptions is { Count: > 0 })
            return HandlingResult.FromExceptions(exceptions);
        
        return aggregateStatus switch
        {
            HandlingStatus.Success => HandlingResult.Success,
            HandlingStatus.Cancelled => HandlingResult.Cancelled,
            _ => HandlingResult.NoHandlers
        };
    }
}
