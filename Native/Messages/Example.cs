using Chopsticks.Messages.Abstractions;
using Chopsticks.Messages.TaskBased;
using System;
using System.Threading;
using System.Threading.Tasks;


namespace Chopsticks.Messages
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

    public class MessageHandler : ISyncMessageHandler<Message>, IDisposable
    {
        private IMessageHandlerRegistrar<Message> _registrar;

        public MessageHandler()//ITaskMessageHandlerRegistrar<Message> registrar)
        {
            //_registrar = registrar;
            //_registrar.Register(this);
        }

        public void Dispose()
        {
            //_registrar.Unregister(this);
        }

        void ISyncMessageHandler<Message>.Handle(Message command)
        {
            Console.WriteLine($"Received OnCommand, Sync, Value: {command.Value}.");
        }
    }

    public class MessageAsyncHandler : ITaskMessageHandler<Message>, IDisposable
    {
        private IMessageHandlerRegistrar<Message> _registrar;

        public MessageAsyncHandler()//ITaskMessageHandlerRegistrar<Message> registrar)
        {
            //_registrar = registrar;
            //_registrar.Register(this);
        }

        public void Dispose()
        {
            //_registrar.Unregister(this);
        }

        async Task ITaskMessageHandler<Message>.HandleAsync(
            Message message, CancellationToken token)
        {
            Console.WriteLine($"Received OnCommand, Async, Value: {message.Value}.");
            await Task.Delay(100); // Simulate async work.
            Console.WriteLine($"Done handling OnCommand, Async, Value: {message.Value}.");
        }
    }
}
