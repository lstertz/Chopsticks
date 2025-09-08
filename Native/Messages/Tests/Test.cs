using Chopsticks.Messages;
using Chopsticks.Messages.Handlers;
using Chopsticks.Messages.Handlers.Multicast;
using Chopsticks.Messages.Registration;

namespace Tests
{
    public class Test
    {
        public class Message
        {
            public string Value { get; set; }
        }

        public class MessageSender
        {
            private readonly IMessageHandler<Message> _handler;

            public MessageSender(IMessageHandler<Message> handler) =>
                _handler = handler;

            public HandlingPromise Send()
            {
                Console.WriteLine("Sending OnCommand");
                var promise = _handler.Handle(new Message())
                    .ThrowIfFailed();
                Console.WriteLine("Sent OnCommand");

                return promise;
            }

            public async Task<HandlingResult> SendAsync()
            {
                Console.WriteLine("Sending OnCommand");
                var result = await _handler.HandleAsync(new Message())
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
            void ISyncMessageHandler<Message>.Handle(Message command)
            {
                Console.WriteLine($"Received OnCommand, Sync, Value: {command.Value}.");
            }
        }

        public class SuccessfulAsyncMessageHandler : ITaskMessageHandler<Message>
        {
            async Task ITaskMessageHandler<Message>.HandleAsync(
                Message message, CancellationToken token)
            {
                Console.WriteLine($"Received OnCommand, Async, Value: {message.Value}.");
                await Task.Delay(100, token); // Simulate async work.
                Console.WriteLine($"Done handling OnCommand, Async, Value: {message.Value}.");
            }
        }

        [Test]
        public async Task Integration_MulticastAsyncCallAllAsync_Passes()
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
            var result = await sender.SendAsync();

            // Assert
            Assert.That(result.Status, Is.EqualTo(HandlingStatus.Success));
        }

        [Test]
        public async Task Integration_MulticastAsyncCallAllAsyncTwice_Passes()
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
        public async Task Integration_MulticastAsyncCallAllSync_Passes()
        {
            // Set up
            var multicastHandler = new MulticastMessageHandler<Message>();
            (multicastHandler as IMessageHandlerRegistrar<Message>).Register(
                new SuccessfulSyncMessageHandler());
            (multicastHandler as IMessageHandlerRegistrar<Message>).Register(
                new SuccessfulSyncMessageHandler());
            (multicastHandler as IMessageHandlerRegistrar<Message>).Register(
                new SuccessfulSyncMessageHandler());

            var sender = new MessageSender(multicastHandler);

            // Act
            var result = await sender.SendAsync();

            // Assert
            Assert.That(result.Status, Is.EqualTo(HandlingStatus.Success));
        }

        [Test]
        public async Task Integration_MulticastAsyncCallAsyncFirst_Passes()
        {
            // Set up
            var multicastHandler = new MulticastMessageHandler<Message>();
            (multicastHandler as IMessageHandlerRegistrar<Message>).Register(
                new SuccessfulAsyncMessageHandler(), new RegistrationSettings()
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
        public async Task Integration_MulticastAsyncCallAsyncSecond_Passes()
        {
            // Set up
            var multicastHandler = new MulticastMessageHandler<Message>();
            (multicastHandler as IMessageHandlerRegistrar<Message>).Register(
                new SuccessfulSyncMessageHandler(), new RegistrationSettings()
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
        public async Task Integration_MulticastSyncCallAllAsync_Passes()
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
            _ = sender.Send();

            await Task.Delay(400);  // Make sure the async handlers finish.

            var promise = sender.Send();

            await Task.Delay(400);

            // Assert
            Assert.That(promise.Status, Is.EqualTo(HandlingStatus.Success));
        }

        [Test]
        public void Integration_MulticastSyncCallAllSync_Passes()
        {
            // Set up
            var multicastHandler = new MulticastMessageHandler<Message>();
            (multicastHandler as IMessageHandlerRegistrar<Message>).Register(
                new SuccessfulSyncMessageHandler());
            (multicastHandler as IMessageHandlerRegistrar<Message>).Register(
                new SuccessfulSyncMessageHandler());
            (multicastHandler as IMessageHandlerRegistrar<Message>).Register(
                new SuccessfulSyncMessageHandler());

            var sender = new MessageSender(multicastHandler);

            // Act
            var promise = sender.Send();

            // All should finish immediately.

            // Assert
            Assert.That(promise.Status, Is.EqualTo(HandlingStatus.Success));
        }

        [Test]
        public async Task Integration_MulticastSyncCallAsyncFirst_Passes()
        {
            // Set up
            var multicastHandler = new MulticastMessageHandler<Message>();
            (multicastHandler as IMessageHandlerRegistrar<Message>).Register(
                new SuccessfulAsyncMessageHandler(), new RegistrationSettings()
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
        public async Task Integration_MulticastSyncCallAsyncSecond_Passes()
        {
            // Set up
            var multicastHandler = new MulticastMessageHandler<Message>();
            (multicastHandler as IMessageHandlerRegistrar<Message>).Register(
                new SuccessfulSyncMessageHandler(), new RegistrationSettings()
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
        public void UniSyncToAsync_Failure_Throws()
        {
            // Set up
            var handler = new FailingAsyncMessageHandler();
            var sender = new MessageSender(handler);

            bool hasCompleted = false;

            // Act & Assert
            Assert.ThrowsAsync<Exception>(async () =>
            {
                var result = sender.Send();
                result.OnCompletion(_ => hasCompleted = true);

                while (!hasCompleted)  // Wait for async completion.
                    await Task.Delay(10);
            });
        }

        [Test]
        public void UniSyncToSync_Failure_Throws()
        {
            // Set up
            var handler = new FailingSyncMessageHandler();
            var sender = new MessageSender(handler);

            // Act & Assert
            Assert.Throws<Exception>(() => sender.Send());
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