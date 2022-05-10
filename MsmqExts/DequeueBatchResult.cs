using System;
using System.Collections.Generic;
using System.Linq;

namespace MsmqExts
{
    public class DequeueBatchResult
    {
        public DequeueBatchResult(IMsmqTransaction transaction)
        {
            Transaction = transaction;
            Messages = new List<IFetchedMessage>();
        }

        public IMsmqTransaction Transaction { get; private set; }

        public List<IFetchedMessage> Messages { get; internal set; }
        public TimeSpan DequeueElapsed { get; internal set; } = new TimeSpan();
        public int NumberOfDequeuedMessages { get; internal set; } = 0;

        public void ThrowIfHasAnBadMessage()
        {
            if (Messages.Any() && Messages[0].DequeueResultStatus == DequeueResultStatus.Exception)
            {
                throw new BatchDequeueResultHasBadMessageException(Messages[0].DequeueException ?? new Exception("Batch dequeue got unknown exception"));
            }            
        }
    }
}
