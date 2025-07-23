using Chopsticks.Messages.Abstractions;
using Chopsticks.Messages.TaskBased;
using System;
using System.Threading;
using System.Threading.Tasks;


namespace Chopsticks.Messages
{
    /// <summary>
    /// Example class for testing purposes.
    /// </summary>
    public class Example
    {
        public async Task Run()
        {
            var sender = new MessageSender(DefaultTaskMessageHandler<Message>.Get());
            var receiver = new MessageHandler(DefaultTaskMessageHandler<Message>.Get());

            await sender.Send();

            receiver.Dispose();
        }
    }



    public class Message
    {
        public string Value { get; set; }
    }


    public class MessageSender
    {
        private ITaskMessageHandler<Message> _handler;

        public MessageSender(ITaskMessageHandler<Message> handler) =>
            _handler = handler;

        public async Task Send()
        {
            Console.WriteLine("Sending OnCommand");
            await _handler.HandleAsync(new Message());
            Console.WriteLine("Sent OnCommand");
        }
    }

    public class MessageHandler : ISyncTaskMessageHandler<Message>, IDisposable
    {
        private ITaskMessageHandlerRegistrar<Message> _registrar;

        public MessageHandler(ITaskMessageHandlerRegistrar<Message> registrar)
        {
            _registrar = registrar;
            _registrar.Register(this);
        }

        public void Dispose()
        {
            _registrar.Unregister(this);
        }

        void ISyncTaskMessageHandler<Message>.Handle(Message command)
        {
            Console.WriteLine($"Received OnCommand, Value: {command.Value}.");
        }
    }

    public class MessageAsyncHandler : IAsyncTaskMessageHandler<Message>, IDisposable
    {
        private ITaskMessageHandlerRegistrar<Message> _registrar;

        public MessageAsyncHandler(ITaskMessageHandlerRegistrar<Message> registrar)
        {
            _registrar = registrar;
            _registrar.Register(this);
        }

        public void Dispose()
        {
            _registrar.Unregister(this);
        }

        async Task IAsyncTaskMessageHandler<Message>.HandleAsync(
            Message message, CancellationToken token)
        {
            Console.WriteLine($"Received OnCommand, Value: {message.Value}.");
        }
    }
}
