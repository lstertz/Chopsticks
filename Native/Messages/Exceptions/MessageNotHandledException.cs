using System;

namespace Chopsticks.Messages.Exceptions
{
    public class MessageNotHandledException : Exception
    {
        public MessageNotHandledException(string? message = null)
            : base(message ?? "A message was not handled.")
        {
        }
    }
}
