using System;

namespace MsmqExts
{
    public class MessageResultIsNullException : Exception
    {
        public MessageResultIsNullException() : base("Message result should not null or empty")
        {

        }

        public MessageResultIsNullException(string message) : base(message)
        {

        }
    }
}
