using System;

#if NET462
using System.Messaging;
#else
using Experimental.System.Messaging;
#endif

namespace MsmqExts
{
    public interface IFetchedMessage : IDisposable
    {
        void CommitTransaction();
        void AbortTransaction();
        string Label { get; }
        Message MsmqMessage { get; }
        DequeueResultStatus DequeueResultStatus { get; }
        Exception DequeueException { get; }
        TimeSpan DequeueElapsed { get; }
    }
}
