using Chopsticks.Messages;

namespace Tests
{
    public class Test
    {

        [Test]
        public void Example_Test_StringMatchesTest()
        {
            // Set up
            var example = new Example();

            // Act & Assert
            Assert.That(example.Test, Is.EqualTo("Test"));
        }
    }
}