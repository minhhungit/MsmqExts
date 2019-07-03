// Inspired by https://www.hangfire.io/

using System;
using System.Messaging;

namespace MsmqExts
{
    public interface IMsmqTransaction : IDisposable
    {
        Message Receive(MessageQueue queue, TimeSpan timeout);

        void Commit();
        void Abort();
    }
}
