using Chopsticks.Messages.TaskBased.Commands;
using System;
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
            var sender = new OnCommandSender();
            var receiver = new OnCommandReceiver();

            await sender.Send();

            receiver.Dispose();
        }
    }



    public class OnCommand
    {
        public string Value { get; set; }
    }



    public class OnCommandSender
    {
        public IAsyncTaskCommandReceiver<OnCommand> Receiver { get; set; } =
            IAsyncTaskCommandReceiver<OnCommand>.All;

        public async Task Send()
        {
            Console.WriteLine("Sending OnCommand");
            await Receiver.ReceiveAsync(new OnCommand());
            Console.WriteLine("Sent OnCommand");
        }
    }

    public class OnCommandReceiver : ITaskCommandReceiver<OnCommand>, IDisposable
    {
        public OnCommandReceiver()
        {
            ITaskCommandReceiver<OnCommand>.Collective.Register(this);
        }

        public void Dispose()
        {
            ITaskCommandReceiver<OnCommand>.Collective.Deregister(this);
        }

        public void Receive(OnCommand command)
        {
            Console.WriteLine($"Received OnCommand, Value: {command.Value}.");
        }

    }
}
