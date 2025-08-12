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
            private IMessageHandler<Message> _handler;

            public MessageSender(IMessageHandler<Message> handler) =>
                _handler = handler;
            public HandlingPromise Send()
            {
                Console.WriteLine("Sending OnCommand");
                var promise = _handler.Handle(new Message());
                Console.WriteLine("Sent OnCommand");

                return promise;
            }

            public async Task<HandlingResult> SendAsync()
            {
                Console.WriteLine("Sending OnCommand");
                var result = await _handler.HandleAsync(new Message());
                Console.WriteLine("Sent OnCommand");

                return result;
            }
        }

        public class SyncMessageHandler : ISyncMessageHandler<Message>
        {
            void ISyncMessageHandler<Message>.Handle(Message command)
            {
                Console.WriteLine($"Received OnCommand, Sync, Value: {command.Value}.");
            }
        }

        public class AsyncMessageHandler : ITaskMessageHandler<Message>
        {
            async Task ITaskMessageHandler<Message>.HandleAsync(
                Message message, CancellationToken token)
            {
                Console.WriteLine($"Received OnCommand, Async, Value: {message.Value}.");
                await Task.Delay(100); // Simulate async work.
                Console.WriteLine($"Done handling OnCommand, Async, Value: {message.Value}.");
            }
        }

        [Test]
        public async Task Integration_MulticastAsyncCallAllAsync_Passes()
        {
            // Set up
            var multicastHandler = new MulticastMessageHandler<Message>();
            (multicastHandler as IMessageHandlerRegistrar<Message>).Register(
                new AsyncMessageHandler());
            (multicastHandler as IMessageHandlerRegistrar<Message>).Register(
                new AsyncMessageHandler());
            (multicastHandler as IMessageHandlerRegistrar<Message>).Register(
                new AsyncMessageHandler());

            var sender = new MessageSender(multicastHandler);

            // Act
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
                new SyncMessageHandler());
            (multicastHandler as IMessageHandlerRegistrar<Message>).Register(
                new SyncMessageHandler());
            (multicastHandler as IMessageHandlerRegistrar<Message>).Register(
                new SyncMessageHandler());

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
                new AsyncMessageHandler(), new RegistrationSettings()
                {
                    Order = -1
                });
            (multicastHandler as IMessageHandlerRegistrar<Message>).Register(
                new SyncMessageHandler());

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
                new SyncMessageHandler(), new RegistrationSettings()
                {
                    Order = -1
                });
            (multicastHandler as IMessageHandlerRegistrar<Message>).Register(
                new AsyncMessageHandler());

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
                new AsyncMessageHandler());
            (multicastHandler as IMessageHandlerRegistrar<Message>).Register(
                new AsyncMessageHandler());
            (multicastHandler as IMessageHandlerRegistrar<Message>).Register(
                new AsyncMessageHandler());

            var sender = new MessageSender(multicastHandler);

            // Act
            var promise = sender.Send();

            await Task.Delay(400);  // Make sure the async handlers finish.

            // Assert
            Assert.That(promise.Status, Is.EqualTo(HandlingStatus.Success));
        }

        [Test]
        public void Integration_MulticastSyncCallAllSync_Passes()
        {
            // Set up
            var multicastHandler = new MulticastMessageHandler<Message>();
            (multicastHandler as IMessageHandlerRegistrar<Message>).Register(
                new SyncMessageHandler());
            (multicastHandler as IMessageHandlerRegistrar<Message>).Register(
                new SyncMessageHandler());
            (multicastHandler as IMessageHandlerRegistrar<Message>).Register(
                new SyncMessageHandler());

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
                new AsyncMessageHandler(), new RegistrationSettings()
                {
                    Order = -1
                });
            (multicastHandler as IMessageHandlerRegistrar<Message>).Register(
                new SyncMessageHandler());

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
                new SyncMessageHandler(), new RegistrationSettings()
                {
                    Order = -1
                });
            (multicastHandler as IMessageHandlerRegistrar<Message>).Register(
                new AsyncMessageHandler());

            var sender = new MessageSender(multicastHandler);

            // Act
            var promise = sender.Send();

            await Task.Delay(200);  // Make sure the async handler finishes.

            // Assert
            Assert.That(promise.Status, Is.EqualTo(HandlingStatus.Success));
        }

        [Test]
        public async Task Integration_UniAsyncToSync_Passes()
        {
            // Set up
            var handler = new SyncMessageHandler();
            var sender = new MessageSender(handler);

            // Act
            var result = await sender.SendAsync();

            // Assert
            Assert.That(result, Is.EqualTo(HandlingResult.Success));
        }

        [Test]
        public async Task Integration_UniAsyncToAsync_Passes()
        {
            // Set up
            var handler = new AsyncMessageHandler();
            var sender = new MessageSender(handler);

            // Act
            var result = await sender.SendAsync();

            // Assert
            Assert.That(result, Is.EqualTo(HandlingResult.Success));
        }
    }
}