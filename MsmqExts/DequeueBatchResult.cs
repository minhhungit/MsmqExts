using System;
using System.Collections.Generic;

namespace MsmqExts
{
    public class DequeueBatchResult
    {
        public DequeueBatchResult()
        {
            GoodMessages = new List<IFetchedMessage>();
        }

        public List<IFetchedMessage> GoodMessages { get; internal set; }
        public IFetchedMessage BadMessage { get; internal set; }
        public TimeSpan DequeueElapsed { get; internal set; } = new TimeSpan();
        public int NumberOfDequeuedMessages { get; internal set; } = 0;

        public void ThrowIfHasAnBadMessage()
        {
            if (BadMessage != null)
            {
                throw new BatchDequeueResultHasBadMessageException(BadMessage.DequeueException);
            }            
        }
    }
}
