using Chopsticks.Messages;
using Chopsticks.Messages.Abstractions;
using Chopsticks.Messages.TaskBased;

namespace Tests
{
    public class Test
    {
        [Test]
        public async Task Integration_MulticastAsyncCall_Passes()
        {
            // Set up
            var multicastHandler = new MulticastMessageHandler<Message>();
            (multicastHandler as IMessageHandlerRegistrar<Message>).Register(
                new MessageAsyncHandler(), default);
            (multicastHandler as IMessageHandlerRegistrar<Message>).Register(
                new MessageHandler(), default);

            var sender = new MessageSender(multicastHandler);

            // Act
            var result = await sender.SendAsync();

            // Assert
            Assert.That(result.Status, Is.EqualTo(HandlingStatus.Success));
        }

        [Test]
        public async Task Integration_MulticastSyncCall_Passes()
        {
            // Set up
            var multicastHandler = new MulticastMessageHandler<Message>();
            (multicastHandler as IMessageHandlerRegistrar<Message>).Register(
                new MessageAsyncHandler(), default);
            (multicastHandler as IMessageHandlerRegistrar<Message>).Register(
                new MessageHandler(), default);

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
            var handler = new MessageHandler();
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
            var handler = new MessageAsyncHandler();
            var sender = new MessageSender(handler);

            // Act
            var result = await sender.SendAsync();

            // Assert
            Assert.That(result, Is.EqualTo(HandlingResult.Success));
        }
    }
}