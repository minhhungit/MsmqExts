using System;
#if NET462
using System.Messaging;
#else
using Experimental.System.Messaging;
#endif

namespace MsmqExts
{
    public interface IMsmqTransaction : IDisposable
    {
        MessageQueueTransaction MessageQueueTransaction { get; }
        Message Receive(MessageQueue queue, TimeSpan timeout);

        void Commit();
        void Abort();
    }
}
