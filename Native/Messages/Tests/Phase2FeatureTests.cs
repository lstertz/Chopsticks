using Chopsticks.Messages;
using Chopsticks.Messages.Handlers;
using Chopsticks.Messages.Handlers.Multicast;
using Chopsticks.Messages.Interceptors;
using Chopsticks.Messages.Registration;
using Chopsticks.Messages.Registration.Handlers;
using Chopsticks.Messages.Registration.Interceptors;

namespace Tests
{
    [TestFixture]
    public class Phase2FeatureTests
    {
        public class TestMessage
        {
            public string? Value { get; set; }
        }

        #region ValueTask Handler Tests

        public class SyncCompletingValueTaskHandler : IValueTaskMessageHandler<TestMessage>
        {
            public int HandleCount { get; private set; }

            public ValueTask HandleAsync(TestMessage message, CancellationToken token = default)
            {
                HandleCount++;
                return ValueTask.CompletedTask;
            }
        }

        public class AsyncCompletingValueTaskHandler : IValueTaskMessageHandler<TestMessage>
        {
            public int HandleCount { get; private set; }
            public int DelayMs { get; set; } = 10;

            public async ValueTask HandleAsync(TestMessage message, CancellationToken token = default)
            {
                await Task.Delay(DelayMs, token);
                HandleCount++;
            }
        }

        public class ThrowingValueTaskHandler : IValueTaskMessageHandler<TestMessage>
        {
            public ValueTask HandleAsync(TestMessage message, CancellationToken token = default)
            {
                throw new InvalidOperationException("Test exception");
            }
        }

        public class CancelledValueTaskHandler : IValueTaskMessageHandler<TestMessage>
        {
            public ValueTask HandleAsync(TestMessage message, CancellationToken token = default)
            {
                throw new OperationCanceledException("Cancelled");
            }
        }

        public class SyncCompletingValueTaskContextHandler : IValueTaskContextHandler<TestMessage, DefaultMessageContext<TestMessage>>
        {
            public int HandleCount { get; private set; }

            public ValueTask HandleAsync(DefaultMessageContext<TestMessage> context)
            {
                HandleCount++;
                return ValueTask.CompletedTask;
            }
        }

        public class AsyncCompletingValueTaskContextHandler : IValueTaskContextHandler<TestMessage, DefaultMessageContext<TestMessage>>
        {
            public int HandleCount { get; private set; }

            public async ValueTask HandleAsync(DefaultMessageContext<TestMessage> context)
            {
                await Task.Delay(10);
                HandleCount++;
            }
        }

        [Test]
        public async Task ValueTaskMessageHandler_SyncCompletion_ReturnsSuccess()
        {
            // Arrange
            var handler = new SyncCompletingValueTaskHandler();
            IMessageHandler<TestMessage> handlerInterface = handler;
            var message = new TestMessage { Value = "test" };

            // Act
            var awaitable = handlerInterface.TryHandleAsync(message, CancellationToken.None);
            var result = await awaitable;

            // Assert
            Assert.That(result.Status, Is.EqualTo(HandlingStatus.Success));
            Assert.That(handler.HandleCount, Is.EqualTo(1));
        }

        [Test]
        public async Task ValueTaskMessageHandler_AsyncCompletion_ReturnsSuccess()
        {
            // Arrange
            var handler = new AsyncCompletingValueTaskHandler { DelayMs = 50 };
            IMessageHandler<TestMessage> handlerInterface = handler;
            var message = new TestMessage { Value = "test" };

            // Act
            var awaitable = handlerInterface.TryHandleAsync(message, CancellationToken.None);
            var result = await awaitable;

            // Assert
            Assert.That(result.Status, Is.EqualTo(HandlingStatus.Success));
            Assert.That(handler.HandleCount, Is.EqualTo(1));
        }

        [Test]
        public async Task ValueTaskMessageHandler_ThrowsException_ReturnsFailure()
        {
            // Arrange
            var handler = new ThrowingValueTaskHandler();
            IMessageHandler<TestMessage> handlerInterface = handler;
            var message = new TestMessage { Value = "test" };

            // Act
            var awaitable = handlerInterface.TryHandleAsync(message, CancellationToken.None);
            var result = await awaitable;

            // Assert
            Assert.That(result.Status, Is.EqualTo(HandlingStatus.Failure));
            Assert.That(result.Exceptions, Is.Not.Empty);
            Assert.That(result.Exceptions.First(), Is.TypeOf<InvalidOperationException>());
        }

        [Test]
        public async Task ValueTaskMessageHandler_Cancelled_ReturnsCancelled()
        {
            // Arrange
            var handler = new CancelledValueTaskHandler();
            IMessageHandler<TestMessage> handlerInterface = handler;
            var message = new TestMessage { Value = "test" };

            // Act
            var awaitable = handlerInterface.TryHandleAsync(message, CancellationToken.None);
            var result = await awaitable;

            // Assert
            Assert.That(result.Status, Is.EqualTo(HandlingStatus.Cancelled));
        }

        [Test]
        public async Task ValueTaskContextHandler_SyncCompletion_ReturnsSuccess()
        {
            // Arrange
            var handler = new SyncCompletingValueTaskContextHandler();
            IContextHandler<TestMessage, DefaultMessageContext<TestMessage>> handlerInterface = handler;
            var context = new DefaultMessageContext<TestMessage> 
            { 
                Message = new TestMessage { Value = "test" } 
            };

            // Act
            var awaitable = handlerInterface.TryHandleAsync(context);
            var result = await awaitable;

            // Assert
            Assert.That(result.Status, Is.EqualTo(HandlingStatus.Success));
            Assert.That(handler.HandleCount, Is.EqualTo(1));
        }

        [Test]
        public async Task ValueTaskContextHandler_AsyncCompletion_ReturnsSuccess()
        {
            // Arrange
            var handler = new AsyncCompletingValueTaskContextHandler();
            IContextHandler<TestMessage, DefaultMessageContext<TestMessage>> handlerInterface = handler;
            var context = new DefaultMessageContext<TestMessage> 
            { 
                Message = new TestMessage { Value = "test" } 
            };

            // Act
            var awaitable = handlerInterface.TryHandleAsync(context);
            var result = await awaitable;

            // Assert
            Assert.That(result.Status, Is.EqualTo(HandlingStatus.Success));
            Assert.That(handler.HandleCount, Is.EqualTo(1));
        }

        [Test]
        public async Task ValueTaskMessageHandler_MultipleCallsSyncCompletion_AllSucceed()
        {
            // Arrange
            var handler = new SyncCompletingValueTaskHandler();
            IMessageHandler<TestMessage> handlerInterface = handler;
            var message = new TestMessage { Value = "test" };
            const int iterations = 100;

            // Act
            for (int i = 0; i < iterations; i++)
            {
                var result = await handlerInterface.TryHandleAsync(message, CancellationToken.None);
                Assert.That(result.Status, Is.EqualTo(HandlingStatus.Success));
            }

            // Assert
            Assert.That(handler.HandleCount, Is.EqualTo(iterations));
        }

        #endregion

        #region RemoveInterceptor Tests

        public class TrackingInterceptor : IContextInterceptor<TestMessage, DefaultMessageContext<TestMessage>>
        {
            public int InterceptCount { get; private set; }

            public HandlingResultAwaitable InterceptAsync(
                DefaultMessageContext<TestMessage> context,
                Func<DefaultMessageContext<TestMessage>, HandlingResultAwaitable> next)
            {
                InterceptCount++;
                return next(context);
            }
        }

        public class SyncTestHandler : ISyncMessageHandler<TestMessage>
        {
            public int HandleCount { get; private set; }

            public void Handle(TestMessage message)
            {
                HandleCount++;
            }
        }

        [Test]
        public async Task RemoveInterceptor_RemovesSuccessfully_InterceptorNotCalled()
        {
            // Arrange
            var multicastHandler = new MulticastMessageHandler<TestMessage>();
            var registrar = multicastHandler as IMessageHandlerRegistrar<TestMessage, DefaultMessageContext<TestMessage>>;
            
            var innerHandler = new SyncTestHandler();
            var interceptor = new TrackingInterceptor();
            
            var registration = registrar!.Register(innerHandler);
            var interceptorRegistration = registration.AddInterceptor(interceptor);

            // Verify interceptor is called initially
            var message = new TestMessage { Value = "test" };
            await multicastHandler.TryHandleAsync(message, CancellationToken.None);
            Assert.That(interceptor.InterceptCount, Is.EqualTo(1));

            // Act - Remove the interceptor
            var removed = registration.RemoveInterceptor(interceptorRegistration);

            // Assert
            Assert.That(removed, Is.True);
            
            // Interceptor should not be called anymore
            await multicastHandler.TryHandleAsync(message, CancellationToken.None);
            Assert.That(interceptor.InterceptCount, Is.EqualTo(1)); // Still 1, not incremented
        }

        [Test]
        public void RemoveInterceptor_NonExistentInterceptor_ReturnsFalse()
        {
            // Arrange
            var multicastHandler = new MulticastMessageHandler<TestMessage>();
            var registrar = multicastHandler as IMessageHandlerRegistrar<TestMessage, DefaultMessageContext<TestMessage>>;
            
            var innerHandler = new SyncTestHandler();
            var interceptor1 = new TrackingInterceptor();
            var interceptor2 = new TrackingInterceptor();
            
            var registration1 = registrar!.Register(innerHandler);
            var interceptorRegistration1 = registration1.AddInterceptor(interceptor1);

            // Create a second handler and interceptor
            var registration2 = registrar.Register(new SyncTestHandler());
            var interceptorRegistration2 = registration2.AddInterceptor(interceptor2);

            // Act - Try to remove interceptor2 from registration1
            var removed = registration1.RemoveInterceptor(interceptorRegistration2);

            // Assert
            Assert.That(removed, Is.False);
        }

        [Test]
        public async Task RemoveInterceptor_MultipleInterceptors_RemovesCorrectOne()
        {
            // Arrange
            var multicastHandler = new MulticastMessageHandler<TestMessage>();
            var registrar = multicastHandler as IMessageHandlerRegistrar<TestMessage, DefaultMessageContext<TestMessage>>;
            
            var innerHandler = new SyncTestHandler();
            var interceptorA = new TrackingInterceptor();
            var interceptorB = new TrackingInterceptor();
            
            var registration = registrar!.Register(innerHandler);
            var regA = registration.AddInterceptor(interceptorA);
            var regB = registration.AddInterceptor(interceptorB);

            // Verify both are called
            var message = new TestMessage { Value = "test" };
            await multicastHandler.TryHandleAsync(message, CancellationToken.None);
            Assert.That(interceptorA.InterceptCount, Is.EqualTo(1));
            Assert.That(interceptorB.InterceptCount, Is.EqualTo(1));

            // Act - Remove interceptor A
            registration.RemoveInterceptor(regA);

            // Second call
            await multicastHandler.TryHandleAsync(message, CancellationToken.None);

            // Assert - A should still be 1, B should be 2
            Assert.That(interceptorA.InterceptCount, Is.EqualTo(1));
            Assert.That(interceptorB.InterceptCount, Is.EqualTo(2));
        }

        [Test]
        public async Task RemoveInterceptor_AllInterceptors_HandlerStillWorks()
        {
            // Arrange
            var multicastHandler = new MulticastMessageHandler<TestMessage>();
            var registrar = multicastHandler as IMessageHandlerRegistrar<TestMessage, DefaultMessageContext<TestMessage>>;
            
            var innerHandler = new SyncTestHandler();
            var interceptor = new TrackingInterceptor();
            
            var registration = registrar!.Register(innerHandler);
            var interceptorReg = registration.AddInterceptor(interceptor);

            // Remove the interceptor
            registration.RemoveInterceptor(interceptorReg);

            // Act - Handler should still work
            var message = new TestMessage { Value = "test" };
            var result = await multicastHandler.TryHandleAsync(message, CancellationToken.None);

            // Assert
            Assert.That(result.Status, Is.EqualTo(HandlingStatus.Success));
            Assert.That(innerHandler.HandleCount, Is.EqualTo(1));
            Assert.That(interceptor.InterceptCount, Is.EqualTo(0)); // Never called
        }

        [Test]
        public void RemoveInterceptor_SameInterceptorTwice_SecondRemovalReturnsFalse()
        {
            // Arrange
            var multicastHandler = new MulticastMessageHandler<TestMessage>();
            var registrar = multicastHandler as IMessageHandlerRegistrar<TestMessage, DefaultMessageContext<TestMessage>>;
            
            var innerHandler = new SyncTestHandler();
            var interceptor = new TrackingInterceptor();
            
            var registration = registrar!.Register(innerHandler);
            var interceptorReg = registration.AddInterceptor(interceptor);

            // Act
            var firstRemoval = registration.RemoveInterceptor(interceptorReg);
            var secondRemoval = registration.RemoveInterceptor(interceptorReg);

            // Assert
            Assert.That(firstRemoval, Is.True);
            Assert.That(secondRemoval, Is.False);
        }

        #endregion

        #region Integration Tests

        [Test]
        public async Task ValueTaskHandler_InMulticastHandler_Works()
        {
            // Arrange
            var multicastHandler = new MulticastMessageHandler<TestMessage>();
            var registrar = multicastHandler as IMessageHandlerRegistrar<TestMessage, DefaultMessageContext<TestMessage>>;
            
            var valueTaskHandler = new SyncCompletingValueTaskHandler();
            registrar!.Register(valueTaskHandler);

            // Act
            var message = new TestMessage { Value = "test" };
            var result = await multicastHandler.TryHandleAsync(message, CancellationToken.None);

            // Assert
            Assert.That(result.Status, Is.EqualTo(HandlingStatus.Success));
            Assert.That(valueTaskHandler.HandleCount, Is.EqualTo(1));
        }

        [Test]
        public async Task ValueTaskHandler_WithInterceptor_Works()
        {
            // Arrange
            var multicastHandler = new MulticastMessageHandler<TestMessage>();
            var registrar = multicastHandler as IMessageHandlerRegistrar<TestMessage, DefaultMessageContext<TestMessage>>;
            
            var valueTaskHandler = new SyncCompletingValueTaskHandler();
            var interceptor = new TrackingInterceptor();
            
            var registration = registrar!.Register(valueTaskHandler);
            registration.AddInterceptor(interceptor);

            // Act
            var message = new TestMessage { Value = "test" };
            var result = await multicastHandler.TryHandleAsync(message, CancellationToken.None);

            // Assert
            Assert.That(result.Status, Is.EqualTo(HandlingStatus.Success));
            Assert.That(valueTaskHandler.HandleCount, Is.EqualTo(1));
            Assert.That(interceptor.InterceptCount, Is.EqualTo(1));
        }

        #endregion
    }
}
