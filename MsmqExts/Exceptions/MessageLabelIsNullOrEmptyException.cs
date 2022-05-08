using System;

namespace MsmqExts
{
    public class MessageLabelIsNullOrEmptyException: Exception
    {
        public MessageLabelIsNullOrEmptyException(): base("Message label should not null or empty")
        {

        }

        public MessageLabelIsNullOrEmptyException(string message) : base(message)
        {

        }
    }
}
