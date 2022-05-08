using System;

namespace MsmqExts
{
    public interface IFetchedMessage : IDisposable
    {
        void CommitTransaction();
        void AbortTransaction();
        string Label { get; }
        object Result { get; }
        DequeueResultStatus DequeueResultStatus { get; }
        Exception DequeueException { get; }
        TimeSpan DequeueElapsed { get; }
    }
}
