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
            var collective = ITaskMessageReceiver<Message>.Collective;
            var registrar = ITaskMessageReceiver<Message>.Collective;

            var sender = new OnCommandSender(collective);
            var receiver = new OnCommandReceiver(registrar);

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
        private ICollectiveTaskMessageReceiver<Message> _registrar;

        public OnCommandReceiver(ICollectiveTaskMessageReceiver<Message> registrar)
        {
            _registrar = registrar;
            _registrar.Register(this);
        }

        public void Dispose()
        {
            _registrar.Deregister(this);
        }

        MessageResult ITaskMessageReceiver<Message>.Receive(Message command)
        {
            Console.WriteLine($"Received OnCommand, Value: {command.Value}.");
            return new MessageResult();
        }
    }

    public class OnCommandAsyncReceiver : ITaskMessageAsyncReceiver<Message>, IDisposable
    {
        private ICollectiveTaskMessageReceiver<Message> _registrar;

        public OnCommandAsyncReceiver(ICollectiveTaskMessageReceiver<Message> registrar)
        {
            _registrar = registrar;
            _registrar.Register(this);
        }

        public void Dispose()
        {
            _registrar.Deregister(this);
        }

        async Task<MessageResult> ITaskMessageAsyncReceiver<Message>.ReceiveAsync(
            Message message, CancellationToken token)
        {
            Console.WriteLine($"Received OnCommand, Value: {message.Value}.");
            return new MessageResult();
        }
    }
}
