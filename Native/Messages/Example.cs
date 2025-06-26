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
            var sender = new MessageSender(ITaskMulticastMessageHandler<Message>.Default);
            var receiver = new MessageHandler(ITaskMulticastMessageHandler<Message>.Default);

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

    public class MessageHandler : ITaskMessageHandler<Message>, IDisposable
    {
        private ITaskMulticastMessageRegistrar<Message> _registrar;

        public MessageHandler(ITaskMulticastMessageRegistrar<Message> registrar)
        {
            _registrar = registrar;
            _registrar.Register(this);
        }

        public void Dispose()
        {
            _registrar.Unregister(this);
        }

        MessageResult ITaskMessageHandler<Message>.Handle(Message command)
        {
            Console.WriteLine($"Received OnCommand, Value: {command.Value}.");
            return MessageResult.Success;
        }
    }

    public class MessageAsyncHandler : ITaskMessageAsyncHandler<Message>, IDisposable
    {
        private ITaskMulticastMessageRegistrar<Message> _registrar;

        public MessageAsyncHandler(ITaskMulticastMessageRegistrar<Message> registrar)
        {
            _registrar = registrar;
            _registrar.Register(this);
        }

        public void Dispose()
        {
            _registrar.Unregister(this);
        }

        async Task<MessageResult> ITaskMessageAsyncHandler<Message>.HandleAsync(
            Message message, CancellationToken token)
        {
            Console.WriteLine($"Received OnCommand, Value: {message.Value}.");
            return MessageResult.Success;
        }
    }
}
