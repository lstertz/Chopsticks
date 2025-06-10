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
        /// <summary>
        /// A test property.
        /// </summary>
        public string Test => "Test";
    }



    public class OnCommand
    {

    }


    public class CollectiveTaskMessageReceiver<TMessage> :
        BaseCollectiveMessageReceiver<IBaseMessageReceiver<TMessage>,
            IBaseAsyncMessageReceiver<TMessage, Task>, TMessage, Task>
    {
        // TODO :: Support registering interceptors for the collective.
        //           Support intercepting before entire run and before each receiver.
        protected IIntercept<TMessage>[] Intercepters => [];


        // TODO :: Note in docs that async handlers are internally managed and awaited.
        public virtual void Receive(TMessage e)  // TODO :: Possibly return result object.
        {
            // TODO :: Progress through both collections based on their registration settings (order).
            foreach (var receiver in Receivers)
                receiver.Receive(e);
            foreach (var receiver in AsyncReceivers)
                _ = receiver.ReceiveAsync(e);  // TODO :: Manage these.
        }

        public virtual async Task ReceiveAsync(TMessage e, 
            CancellationToken token = default, bool runParallel = false)
        {
            // TODO :: Progress through both collections based on their registration settings (order).
            // TODO :: Account for the setting of parallel handling and cancellation.
            foreach (var receiver in Receivers)
                receiver.Receive(e);
            foreach (var receiver in AsyncReceivers)
                await receiver.ReceiveAsync(e);
        }
    }

    public class CollectiveTaskCommandReceiver<TCommand> :
        CollectiveTaskMessageReceiver<TCommand>,
        ITaskCommandReceiver<TCommand>,
        IAsyncTaskCommandReceiver<TCommand>
    {
        // TODO :: Support command-specific collective settings.

        public override void Receive(TCommand e)
        {
            // TODO :: Apply command-specific constraints to the handling.
            base.Receive(e);
        }

        public override async Task ReceiveAsync(TCommand e,
            CancellationToken token = default, bool runParallel = false)
        {
            // TODO :: Apply command-specific constraints to the handling.
            await base.ReceiveAsync(e, token, runParallel);
        }
    }

    public interface ITaskCommandReceiver<TMessage> :
        IMessageReceiver<TMessage, CollectiveTaskCommandReceiver<TMessage>>
    {
    }

    public interface IAsyncTaskCommandReceiver<TMessage> :
        IAsyncMessageReceiver<TMessage, Task, CollectiveTaskCommandReceiver<TMessage>>
    {
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

    public class OnCommandReceiver : ITaskCommandReceiver<OnCommand>
    {
        public OnCommandReceiver()
        {
            ITaskCommandReceiver<OnCommand>.Collective.Register(this);
        }

        public void Dispose()
        {
            ITaskCommandReceiver<OnCommand>.Collective.Deregister(this);
        }

        public void Receive(OnCommand t)
        {
            Console.WriteLine("Received OnCommand");
        }

    }
}
