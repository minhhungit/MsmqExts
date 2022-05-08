using System;

namespace SimpleMessage
{
    public class MessageHasNoHandlerException: Exception
    {
        public MessageHasNoHandlerException(): base("Message has no handler")
        {

        }
    }
}
