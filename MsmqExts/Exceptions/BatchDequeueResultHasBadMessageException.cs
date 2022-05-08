using System;

namespace MsmqExts
{
    public class BatchDequeueResultHasBadMessageException : Exception
    {
        public Exception BadMessageException { get; set; }

        public BatchDequeueResultHasBadMessageException(Exception badMessageException) : base($"There is a BadMessage in batch dequeued messages, please review it, error detail: {badMessageException?.Message ?? String.Empty}")
        {
            BadMessageException = badMessageException;
        }
    }
}
