using Chopsticks.Messages;

namespace Tests
{
    public class Test
    {
        [Test]
        public async Task Integration_UniAsyncToSync_Passes()
        {
            // Set up
            var handler = new MessageHandler();
            var sender = new MessageSender(handler);

            // Act
            var result = await sender.Send();

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
            var result = await sender.Send();

            // Assert
            Assert.That(result, Is.EqualTo(HandlingResult.Success));
        }
    }
}