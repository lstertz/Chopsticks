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
            var sender = new MessageSender(IMulticastTaskMessageHandler<Message>.Default);
            var receiver = new MessageHandler(IMulticastTaskMessageHandler<Message>.Default);

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
        private ISyncTaskMessageHandler<Message> _handler;

        public MessageSender(ISyncTaskMessageHandler<Message> handler) =>
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

        HandlingPromise ISyncTaskMessageHandler<Message>.Handle(Message command)
        {
            Console.WriteLine($"Received OnCommand, Value: {command.Value}.");
            return HandlingPromise.Success;
        }
    }

    public class MessageAsyncHandler : ITaskMessageHandler<Message>, IDisposable
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

        async Task<HandlingResult> ITaskMessageHandler<Message>.HandleAsync(
            Message message, CancellationToken token)
        {
            Console.WriteLine($"Received OnCommand, Value: {message.Value}.");
            return HandlingResult.Success;
        }
    }
}
