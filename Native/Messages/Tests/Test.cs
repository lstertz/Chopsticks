using Chopsticks.Messages;
using Chopsticks.Messages.Handlers;
using Chopsticks.Messages.Handlers.Multicast;
using Chopsticks.Messages.Interceptors;
using Chopsticks.Messages.Registration;
using System.Collections.Concurrent;
using static Tests.Test;

namespace Tests
{
    public class Test
    {
        public class Message
        {
            public string Value { get; set; }
        }
        public class TestSynchronizationContext : SynchronizationContext
        {
            private readonly ConcurrentQueue<(SendOrPostCallback, object?)> _queue = new();

            public override void Post(SendOrPostCallback d, object? state)
            {
                _queue.Enqueue((d, state));
            }

            public void Run()
            {
                while (_queue.TryDequeue(out var work))
                {
                    work.Item1(work.Item2);
                }
            }
        }

        public class Interceptor : ITaskContextInterceptor<Message, DefaultMessageContext<Message>>
        {
            public bool CalledAfterNext { get; private set; } = false;
            public bool CalledBeforeNext { get; private set; } = false;

            public bool CallNext { get; set; } = true;
            public bool ThrowException { get; set; } = false;

            public Action OnInterception { get; set; } = () => { };

            public async Task InterceptAsync(DefaultMessageContext<Message> context, 
                Func<DefaultMessageContext<Message>, HandlingAwaitable> next)
            {
                Console.WriteLine("Before Interceptor");
                CalledBeforeNext = true;

                if (ThrowException)
                    throw new Exception("Simulated failure in interceptor.");

                OnInterception();

                if (CallNext)
                    await next(context);

                CalledAfterNext = true;
                Console.WriteLine("After Interceptor");
            }
        }

        public class MessageSender
        {
            private readonly IMessageHandler<Message> _handler;

            public MessageSender(IMessageHandler<Message> handler) =>
                _handler = handler;

            public HandlingPromise Send(SynchronizationContext? context = null)
            {
                Console.WriteLine("Sending OnCommand");
                var promise = _handler.Handle(new Message())
                    .ThrowIfFailed(context);
                Console.WriteLine("Sent OnCommand");

                return promise;
            }

            public async Task<HandlingResult> SendAsync()
            {
                Console.WriteLine("Sending OnCommand");
                var result = await _handler.HandleAsync(new Message())
                    .ContinueWithTask()
                    .ThrowIfFailed();
                Console.WriteLine("Sent OnCommand");

                return result;
            }
        }

        public class FailingSyncMessageHandler : ISyncMessageHandler<Message>
        {
            void ISyncMessageHandler<Message>.Handle(Message command)
            {
                throw new Exception("Simulated failure in sync handler.");
            }
        }

        public class FailingAsyncMessageHandler : ITaskMessageHandler<Message>
        {
            async Task ITaskMessageHandler<Message>.HandleAsync(
                Message message, CancellationToken token)
            {
                await Task.Delay(100, token); // Simulate async work.
                throw new Exception("Simulated failure in async handler.");
            }
        }

        public class SuccessfulSyncMessageHandler : ISyncMessageHandler<Message>
        {
            public bool HandledMessage { get; private set; } = false;

            public Action OnHandling { get; set; } = () => { };

            void ISyncMessageHandler<Message>.Handle(Message command)
            {
                Console.WriteLine($"Received OnCommand, Sync, Value: {command.Value}.");
                OnHandling();
                HandledMessage = true;
            }
        }

        public class SuccessfulAsyncMessageHandler : ITaskMessageHandler<Message>
        {
            public bool HandledMessage { get; private set; } = false;

            public Action OnHandling { get; set; } = () => { };

            async Task ITaskMessageHandler<Message>.HandleAsync(
                Message message, CancellationToken token)
            {
                Console.WriteLine($"Received OnCommand, Async, Value: {message.Value}.");
                await Task.Delay(100, token); // Simulate async work.
                Console.WriteLine($"Done handling OnCommand, Async, Value: {message.Value}.");

                OnHandling();

                HandledMessage = true;
            }
        }


        [Test]
        public async Task Integration_WithDispatchInterceptorFailing_FailsWithoutHandling()
        {
            // Set up
            var handlerA = new SuccessfulSyncMessageHandler();
            var handlerB = new SuccessfulAsyncMessageHandler();
            var multicastHandler = new MulticastMessageHandler<Message>();
            (multicastHandler as IMessageHandlerRegistrar<Message>).Register(handlerA);
            (multicastHandler as IMessageHandlerRegistrar<Message>).Register(handlerB);

            var interceptor = new Interceptor()
            {
                ThrowException = true
            };
            multicastHandler.AddDispatchInterceptor(interceptor);

            var sender = new MessageSender(multicastHandler);

            // Act & Assert
            Assert.ThrowsAsync<Exception>(sender.SendAsync);

            Assert.That(handlerA.HandledMessage, Is.False);
            Assert.That(handlerB.HandledMessage, Is.False);

            Assert.That(interceptor.CalledBeforeNext, Is.True);
            Assert.That(interceptor.CalledAfterNext, Is.False);
        }

        [Test]
        public async Task Integration_WithDispatchInterceptors_ExecutesThroughInterceptorsInImplicitOrder()
        {
            // Set up
            var multicastHandler = new MulticastMessageHandler<Message>();
            (multicastHandler as IMessageHandlerRegistrar<Message>).Register(
                new SuccessfulSyncMessageHandler());
            (multicastHandler as IMessageHandlerRegistrar<Message>).Register(
                new SuccessfulAsyncMessageHandler());

            int currentExecution = 0;

            int interceptorExecutionOrderA = -1;
            var interceptorA = new Interceptor()
            {
                OnInterception = () =>
                {
                    interceptorExecutionOrderA = currentExecution++;
                }
            };

            int interceptorExecutionOrderB = -1;
            var interceptorB = new Interceptor()
            {
                OnInterception = () =>
                {
                    interceptorExecutionOrderB = currentExecution++;
                }
            };

            multicastHandler.AddDispatchInterceptor(interceptorA);
            multicastHandler.AddDispatchInterceptor(interceptorB);

            var sender = new MessageSender(multicastHandler);

            // Act
            var result = await sender.SendAsync();

            // Assert
            Assert.That(result.Status, Is.EqualTo(HandlingStatus.Success));

            Assert.That(interceptorA.CalledBeforeNext, Is.True);
            Assert.That(interceptorA.CalledAfterNext, Is.True);
            Assert.That(interceptorExecutionOrderA, Is.EqualTo(0));

            Assert.That(interceptorB.CalledBeforeNext, Is.True);
            Assert.That(interceptorB.CalledAfterNext, Is.True);
            Assert.That(interceptorExecutionOrderB, Is.EqualTo(1));
        }

        [Test]
        public async Task Integration_WithDispatchInterceptorStoppingRun_SuccessfulWithoutHandling()
        {
            // Set up
            var handlerA = new SuccessfulSyncMessageHandler();
            var handlerB = new SuccessfulAsyncMessageHandler();
            var multicastHandler = new MulticastMessageHandler<Message>();
            (multicastHandler as IMessageHandlerRegistrar<Message>).Register(handlerA);
            (multicastHandler as IMessageHandlerRegistrar<Message>).Register(handlerB);

            var interceptor = new Interceptor()
            {
                CallNext = false
            };
            multicastHandler.AddDispatchInterceptor(interceptor);

            var sender = new MessageSender(multicastHandler);

            // Act
            var result = await sender.SendAsync();

            // Assert
            Assert.That(result.Status, Is.EqualTo(HandlingStatus.Success));
            Assert.That(handlerA.HandledMessage, Is.False);
            Assert.That(handlerB.HandledMessage, Is.False);

            Assert.That(interceptor.CalledBeforeNext, Is.True);
            Assert.That(interceptor.CalledAfterNext, Is.True);
        }


        [Test]
        public async Task Integration_WithHandlerInterceptorFailing_FailsWithoutHandling()
        {
            // Set up
            var interceptor = new Interceptor()
            {
                ThrowException = true
            };

            var handler = new SuccessfulAsyncMessageHandler();
            var multicastHandler = new MulticastMessageHandler<Message>();
            (multicastHandler as IMessageHandlerRegistrar<Message, DefaultMessageContext<Message>>)
                .Register(handler)
                .AddInterceptor(interceptor);

            multicastHandler.AddDispatchInterceptor(interceptor);

            var sender = new MessageSender(multicastHandler);

            // Act & Assert
            Assert.ThrowsAsync<Exception>(sender.SendAsync);

            Assert.That(handler.HandledMessage, Is.False);

            Assert.That(interceptor.CalledBeforeNext, Is.True);
            Assert.That(interceptor.CalledAfterNext, Is.False);
        }

        [Test]
        public async Task Integration_WithHandlerInterceptors_ExecutesThroughInterceptorsInImplicitOrder()
        {
            // Set up
            int currentExecution = 0;

            int interceptorExecutionOrderA = -1;
            var interceptorA = new Interceptor()
            {
                OnInterception = () =>
                {
                    interceptorExecutionOrderA = currentExecution++;
                }
            };

            int interceptorExecutionOrderB = -1;
            var interceptorB = new Interceptor()
            {
                OnInterception = () =>
                {
                    interceptorExecutionOrderB = currentExecution++;
                }
            };

            var multicastHandler = new MulticastMessageHandler<Message>();
            (multicastHandler as IMessageHandlerRegistrar<Message, DefaultMessageContext<Message>>)
                .Register(new SuccessfulSyncMessageHandler())
                .AddInterceptor(interceptorA)
                .AddInterceptor(interceptorB);

            var sender = new MessageSender(multicastHandler);

            // Act
            var result = await sender.SendAsync();

            // Assert
            Assert.That(result.Status, Is.EqualTo(HandlingStatus.Success));

            Assert.That(interceptorA.CalledBeforeNext, Is.True);
            Assert.That(interceptorA.CalledAfterNext, Is.True);
            Assert.That(interceptorExecutionOrderA, Is.EqualTo(0));

            Assert.That(interceptorB.CalledBeforeNext, Is.True);
            Assert.That(interceptorB.CalledAfterNext, Is.True);
            Assert.That(interceptorExecutionOrderB, Is.EqualTo(1));
        }

        [Test]
        public async Task Integration_WithHandlerInterceptorStoppingRun_SuccessfulWithoutHandling()
        {
            // Set up
            var interceptor = new Interceptor()
            {
                CallNext = false
            };

            var handler = new SuccessfulSyncMessageHandler();
            var multicastHandler = new MulticastMessageHandler<Message>();
            (multicastHandler as IMessageHandlerRegistrar<Message, DefaultMessageContext<Message>>)
                .Register(handler)
                .AddInterceptor(interceptor);

            var sender = new MessageSender(multicastHandler);

            // Act
            var result = await sender.SendAsync();

            // Assert
            Assert.That(result.Status, Is.EqualTo(HandlingStatus.Success));
            Assert.That(handler.HandledMessage, Is.False);

            Assert.That(interceptor.CalledBeforeNext, Is.True);
            Assert.That(interceptor.CalledAfterNext, Is.True);
        }


        [Test]
        public async Task Integration_MulticastAsyncCallAllAsync_SucceedsAllInImplicitOrder()
        {
            // Set up
            int executionOrder = 0;

            int handlerExecutionOrderA = -1;
            var multicastHandler = new MulticastMessageHandler<Message>();
            (multicastHandler as IMessageHandlerRegistrar<Message>).Register(
                new SuccessfulAsyncMessageHandler()
                {
                    OnHandling = () =>
                    {
                        handlerExecutionOrderA = executionOrder++;
                    }
                });

            int handlerExecutionOrderB = -1;
            (multicastHandler as IMessageHandlerRegistrar<Message>).Register(
                new SuccessfulAsyncMessageHandler()
                {
                    OnHandling = () =>
                    {
                        handlerExecutionOrderB = executionOrder++;
                    }
                });

            int handlerExecutionOrderC = -1;
            (multicastHandler as IMessageHandlerRegistrar<Message>).Register(
                new SuccessfulAsyncMessageHandler()
                {
                    OnHandling = () =>
                    {
                        handlerExecutionOrderC = executionOrder++;
                    }
                });

            var sender = new MessageSender(multicastHandler);

            // Act
            var result = await sender.SendAsync();

            // Assert
            Assert.That(result.Status, Is.EqualTo(HandlingStatus.Success));

            Assert.That(handlerExecutionOrderA, Is.EqualTo(0));
            Assert.That(handlerExecutionOrderB, Is.EqualTo(1));
            Assert.That(handlerExecutionOrderC, Is.EqualTo(2));
        }

        [Test]
        public async Task Integration_MulticastAsyncCallAllAsyncTwice_SucceedsAll()
        {
            // Set up
            var multicastHandler = new MulticastMessageHandler<Message>();
            (multicastHandler as IMessageHandlerRegistrar<Message>).Register(
                new SuccessfulAsyncMessageHandler());
            (multicastHandler as IMessageHandlerRegistrar<Message>).Register(
                new SuccessfulAsyncMessageHandler());
            (multicastHandler as IMessageHandlerRegistrar<Message>).Register(
                new SuccessfulAsyncMessageHandler());

            var sender = new MessageSender(multicastHandler);

            // Act
            _ = await sender.SendAsync();
            var result = await sender.SendAsync();

            // Assert
            Assert.That(result.Status, Is.EqualTo(HandlingStatus.Success));
        }

        [Test]
        public async Task Integration_MulticastAsyncCallAllSync_SucceedsAllInImplicitOrder()
        {
            // Set up
            int executionOrder = 0;

            int handlerExecutionOrderA = -1;
            var multicastHandler = new MulticastMessageHandler<Message>();
            (multicastHandler as IMessageHandlerRegistrar<Message>).Register(
                new SuccessfulSyncMessageHandler()
                {
                    OnHandling = () =>
                    {
                        handlerExecutionOrderA = executionOrder++;
                    }
                });

            int handlerExecutionOrderB = -1;
            (multicastHandler as IMessageHandlerRegistrar<Message>).Register(
                new SuccessfulSyncMessageHandler()
                {
                    OnHandling = () =>
                    {
                        handlerExecutionOrderB = executionOrder++;
                    }
                });

            int handlerExecutionOrderC = -1;
            (multicastHandler as IMessageHandlerRegistrar<Message>).Register(
                new SuccessfulSyncMessageHandler()
                {
                    OnHandling = () =>
                    {
                        handlerExecutionOrderC = executionOrder++;
                    }
                });

            var sender = new MessageSender(multicastHandler);

            // Act
            var result = await sender.SendAsync();

            // Assert
            Assert.That(result.Status, Is.EqualTo(HandlingStatus.Success));

            Assert.That(handlerExecutionOrderA, Is.EqualTo(0));
            Assert.That(handlerExecutionOrderB, Is.EqualTo(1));
            Assert.That(handlerExecutionOrderC, Is.EqualTo(2));
        }

        [Test]
        public async Task Integration_MulticastAsyncCallAsyncFirst_SucceedsAll()
        {
            // Set up
            var multicastHandler = new MulticastMessageHandler<Message>();
            (multicastHandler as IMessageHandlerRegistrar<Message>).Register(
                new SuccessfulAsyncMessageHandler(), new HandlerRegistrationSettings()
                {
                    Order = -1
                });
            (multicastHandler as IMessageHandlerRegistrar<Message>).Register(
                new SuccessfulSyncMessageHandler());

            var sender = new MessageSender(multicastHandler);

            // Act
            var result = await sender.SendAsync();

            // Assert
            Assert.That(result.Status, Is.EqualTo(HandlingStatus.Success));
        }

        [Test]
        public async Task Integration_MulticastAsyncCallAsyncSecond_SucceedsAll()
        {
            // Set up
            var multicastHandler = new MulticastMessageHandler<Message>();
            (multicastHandler as IMessageHandlerRegistrar<Message>).Register(
                new SuccessfulSyncMessageHandler(), new HandlerRegistrationSettings()
                {
                    Order = -1
                });
            (multicastHandler as IMessageHandlerRegistrar<Message>).Register(
                new SuccessfulAsyncMessageHandler());

            var sender = new MessageSender(multicastHandler);

            // Act
            var result = await sender.SendAsync();

            // Assert
            Assert.That(result.Status, Is.EqualTo(HandlingStatus.Success));
        }

        [Test]
        public async Task Integration_MulticastSyncCallAllAsync_SucceedsAllInImplicitOrder()
        {
            // Set up
            int executionOrder = 0;

            int handlerExecutionOrderA = -1;
            var multicastHandler = new MulticastMessageHandler<Message>();
            (multicastHandler as IMessageHandlerRegistrar<Message>).Register(
                new SuccessfulAsyncMessageHandler()
                {
                    OnHandling = () =>
                    {
                        handlerExecutionOrderA = executionOrder++;
                    }
                });

            int handlerExecutionOrderB = -1;
            (multicastHandler as IMessageHandlerRegistrar<Message>).Register(
                new SuccessfulAsyncMessageHandler()
                {
                    OnHandling = () =>
                    {
                        handlerExecutionOrderB = executionOrder++;
                    }
                });

            int handlerExecutionOrderC = -1;
            (multicastHandler as IMessageHandlerRegistrar<Message>).Register(
                new SuccessfulAsyncMessageHandler()
                {
                    OnHandling = () =>
                    {
                        handlerExecutionOrderC = executionOrder++;
                    }
                });

            var sender = new MessageSender(multicastHandler);

            // Act
            var promise = sender.Send();

            await Task.Delay(400);  // Make sure the async handlers finish.

            // Assert
            Assert.That(promise.Status, Is.EqualTo(HandlingStatus.Success));

            Assert.That(handlerExecutionOrderA, Is.EqualTo(0));
            Assert.That(handlerExecutionOrderB, Is.EqualTo(1));
            Assert.That(handlerExecutionOrderC, Is.EqualTo(2));
        }

        [Test]
        public void Integration_MulticastSyncCallAllSync_SucceedsAllInImplicitOrder()
        {
            // Set up
            int executionOrder = 0;

            int handlerExecutionOrderA = -1;
            var multicastHandler = new MulticastMessageHandler<Message>();
            (multicastHandler as IMessageHandlerRegistrar<Message>).Register(
                new SuccessfulSyncMessageHandler()
                {
                    OnHandling = () =>
                    {
                        handlerExecutionOrderA = executionOrder++;
                    }
                });

            int handlerExecutionOrderB = -1;
            (multicastHandler as IMessageHandlerRegistrar<Message>).Register(
                new SuccessfulSyncMessageHandler()
                {
                    OnHandling = () =>
                    {
                        handlerExecutionOrderB = executionOrder++;
                    }
                });

            int handlerExecutionOrderC = -1;
            (multicastHandler as IMessageHandlerRegistrar<Message>).Register(
                new SuccessfulSyncMessageHandler()
                {
                    OnHandling = () =>
                    {
                        handlerExecutionOrderC = executionOrder++;
                    }
                });

            var sender = new MessageSender(multicastHandler);

            // Act
            var promise = sender.Send();

            // All should finish immediately.

            // Assert
            Assert.That(promise.Status, Is.EqualTo(HandlingStatus.Success));

            Assert.That(handlerExecutionOrderA, Is.EqualTo(0));
            Assert.That(handlerExecutionOrderB, Is.EqualTo(1));
            Assert.That(handlerExecutionOrderC, Is.EqualTo(2));
        }

        [Test]
        public async Task Integration_MulticastSyncCallAsyncFirst_SucceedsAll()
        {
            // Set up
            var multicastHandler = new MulticastMessageHandler<Message>();
            (multicastHandler as IMessageHandlerRegistrar<Message>).Register(
                new SuccessfulAsyncMessageHandler(), new HandlerRegistrationSettings()
                {
                    Order = -1
                });
            (multicastHandler as IMessageHandlerRegistrar<Message>).Register(
                new SuccessfulSyncMessageHandler());

            var sender = new MessageSender(multicastHandler);

            // Act
            var promise = sender.Send();

            await Task.Delay(200);  // Make sure the async handler finishes.

            // Assert
            Assert.That(promise.Status, Is.EqualTo(HandlingStatus.Success));
        }

        [Test]
        public async Task Integration_MulticastSyncCallAsyncSecond_SucceedsAll()
        {
            // Set up
            var multicastHandler = new MulticastMessageHandler<Message>();
            (multicastHandler as IMessageHandlerRegistrar<Message>).Register(
                new SuccessfulSyncMessageHandler(), new HandlerRegistrationSettings()
                {
                    Order = -1
                });
            (multicastHandler as IMessageHandlerRegistrar<Message>).Register(
                new SuccessfulAsyncMessageHandler());

            var sender = new MessageSender(multicastHandler);

            // Act
            var promise = sender.Send();

            await Task.Delay(200);  // Make sure the async handler finishes.
            promise = sender.Send();

            await Task.Delay(200);  // Make sure the async handler finishes.
            promise = sender.Send();

            await Task.Delay(200);  // Make sure the async handler finishes.

            // Assert
            Assert.That(promise.Status, Is.EqualTo(HandlingStatus.Success));
        }


        [Test]
        public void UniAsyncToAsync_Failure_Throws()
        {
            // Set up
            var handler = new FailingAsyncMessageHandler();
            var sender = new MessageSender(handler);

            // Act & Assert
            Assert.ThrowsAsync<Exception>(sender.SendAsync);
        }

        [Test]
        public void UniAsyncToSync_Failure_Throws()
        {
            // Set up
            var handler = new FailingSyncMessageHandler();
            var sender = new MessageSender(handler);

            // Act
            Assert.ThrowsAsync<Exception>(sender.SendAsync);
        }

        [Test]
        public async Task UniSyncToAsync_Failure_Throws()
        {
            // Set up
            var handler = new FailingAsyncMessageHandler();
            var sender = new MessageSender(handler);

            bool hasCompleted = false;

            var context = new TestSynchronizationContext();
            //SynchronizationContext.SetSynchronizationContext(context);

            // Act
            var result = sender.Send(context);
            result.OnCompletion(_ => hasCompleted = true);

            while (!hasCompleted)  // Wait for async completion.
                await Task.Delay(10);

            // Assert
            Assert.Throws<Exception>(context.Run);
        }

        [Test]
        public void UniSyncToSync_Failure_Throws()
        {
            // Set up
            var handler = new FailingSyncMessageHandler();
            var sender = new MessageSender(handler);

            // Act & Assert
            Assert.Throws<Exception>(() => sender.Send()); // Throws synchronously without context.
        }

        [Test]
        public async Task UniAsyncToAsync_Successful_Passes()
        {
            // Set up
            var handler = new SuccessfulAsyncMessageHandler();
            var sender = new MessageSender(handler);

            // Act
            var result = await sender.SendAsync();

            // Assert
            Assert.That(result.Status, Is.EqualTo(HandlingStatus.Success));
        }


        [Test]
        public async Task UniAsyncToSync_Successful_Passes()
        {
            // Set up
            var handler = new SuccessfulSyncMessageHandler();
            var sender = new MessageSender(handler);

            // Act
            var result = await sender.SendAsync();

            // Assert
            Assert.That(result.Status, Is.EqualTo(HandlingStatus.Success));
        }

        [Test]
        public async Task UniSyncToAsync_Successful_Passes()
        {
            // Set up
            var handler = new SuccessfulAsyncMessageHandler();
            var sender = new MessageSender(handler);

            bool hasCompleted = false;

            // Act
            var result = sender.Send();
            result.OnCompletion(_ => hasCompleted = true);

            while (!hasCompleted)  // Wait for async completion.
                await Task.Delay(10);

            // Assert
            Assert.That(result.Status, Is.EqualTo(HandlingStatus.Success));
        }

        [Test]
        public void UniSyncToSync_Successful_Passes()
        {
            // Set up
            var handler = new SuccessfulSyncMessageHandler();
            var sender = new MessageSender(handler);

            // Act
            var result = sender.Send();

            // Assert
            Assert.That(result.Status, Is.EqualTo(HandlingStatus.Success));
        }
    }
}