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
            var sender = new OnCommandSender(ITaskMessageReceiver<Message>.DefaultCollective);
            var receiver = new OnCommandReceiver(ITaskMessageReceiver<Message>.DefaultCollective);

            await sender.Send();

            receiver.Dispose();
        }
    }



    public class Message
    {
        public string Value { get; set; }
    }


    public class OnCommandSender
    {
        private ITaskMessageReceiver<Message> _receiver;

        public OnCommandSender(ITaskMessageReceiver<Message> receiver) =>
            _receiver = receiver;

        public async Task Send()
        {
            Console.WriteLine("Sending OnCommand");
            await _receiver.ReceiveAsync(new Message());
            Console.WriteLine("Sent OnCommand");
        }
    }

    public class OnCommandReceiver : ITaskMessageReceiver<Message>, IDisposable
    {
        private ITaskMessageReceiverCollective<Message> _registrar;

        public OnCommandReceiver(ITaskMessageReceiverCollective<Message> registrar)
        {
            _registrar = registrar;
            _registrar.Register(this);
        }

        public void Dispose()
        {
            _registrar.Unregister(this);
        }

        MessageResult ITaskMessageReceiver<Message>.Receive(Message command)
        {
            Console.WriteLine($"Received OnCommand, Value: {command.Value}.");
            return new MessageResult();
        }
    }

    public class OnCommandAsyncReceiver : ITaskMessageAsyncReceiver<Message>, IDisposable
    {
        private ITaskMessageReceiverCollective<Message> _registrar;

        public OnCommandAsyncReceiver(ITaskMessageReceiverCollective<Message> registrar)
        {
            _registrar = registrar;
            _registrar.Register(this);
        }

        public void Dispose()
        {
            _registrar.Unregister(this);
        }

        async Task<MessageResult> ITaskMessageAsyncReceiver<Message>.ReceiveAsync(
            Message message, CancellationToken token)
        {
            Console.WriteLine($"Received OnCommand, Value: {message.Value}.");
            return new MessageResult();
        }
    }
}
