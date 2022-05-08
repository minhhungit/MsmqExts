﻿using System;

namespace MsmqExts
{
    public class MsmqFetchedMessage : IFetchedMessage
    {
        public IMsmqTransaction Transaction { get; private set; }

        public MsmqFetchedMessage(IMsmqTransaction transaction, string label, object result, DequeueResultStatus dequeueResultStatus, Exception dequeueEx)
        {
            Transaction = transaction;
            Label = label;
            Result = result;
            DequeueResultStatus = dequeueResultStatus;

            if (dequeueResultStatus == DequeueResultStatus.Success)
            {
                Transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));

                if (string.IsNullOrWhiteSpace(label))
                {
                    // no need to abort transaction here because exception will be caught and transation will be aborted later
                    DequeueException = new MessageLabelIsNullOrEmptyException();
                    throw DequeueException;
                }

                if (result == null)
                {
                    // no need to abort transaction here because exception will be caught and transation will be aborted later
                    DequeueException = new MessageResultIsNullException();
                    throw DequeueException;
                }
            }
            else
            {
                DequeueException = dequeueEx;
            }
        }

        /// <summary>
        /// Label of message
        /// </summary>
        public string Label { get; private set; }

        /// <summary>
        /// Message object
        /// </summary>
        public object Result { get; private set; }

        public DequeueResultStatus DequeueResultStatus { get; private set; }

        public Exception DequeueException { get; private set; }

        public TimeSpan DequeueElapsed { get; private set; }

        public void CommitTransaction()
        {
            Transaction.Commit();
        }

        public void AbortTransaction()
        {
            Transaction.Abort();
        }

        public void Dispose()
        {
            Transaction.Dispose();
        }

        internal void SetDequeueElapsed(TimeSpan elapsed)
        {
            DequeueElapsed = elapsed;
        }
    }
}
