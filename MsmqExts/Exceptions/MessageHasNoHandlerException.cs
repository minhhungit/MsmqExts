using System;

namespace MsmqExts
{
    public class MsmqMessageHasNoHandlerException: Exception
    {
        public MsmqMessageHasNoHandlerException(): base("Message has no handler")
        {

        }
    }
}
