using System;
#if NET462
using System.Messaging;
#else
using Experimental.System.Messaging;
#endif

namespace MsmqExts
{
    public class MsmqInternalTransaction : IMsmqTransaction
    {
        public MessageQueueTransaction MessageQueueTransaction { get; }

        public MsmqInternalTransaction()
        {
            MessageQueueTransaction = new MessageQueueTransaction();
        }

        public void Dispose()
        {
            MessageQueueTransaction.Dispose();
        }

        /// <summary>
        /// Receive message
        /// </summary>
        /// <param name="queue">MSMQ queue</param>
        /// <param name="timeout">Receive timeout</param>
        /// <returns></returns>
        public Message Receive(MessageQueue queue, TimeSpan timeout)
        {
            if (queue.Transactional)
            {
                if (MessageQueueTransaction.Status == MessageQueueTransactionStatus.Initialized)
                {
                    MessageQueueTransaction.Begin();
                }
                
                return queue.Receive(timeout, MessageQueueTransaction);
            }
            else
            {
                throw new Exception("MSMQ Queue must be transaction");
            }
        }

        /// <summary>
        /// Commit a message, message will be remove out of queue
        /// </summary>
        public void Commit()
        {
            if (MessageQueueTransaction.Status == MessageQueueTransactionStatus.Pending)
            {
                MessageQueueTransaction.Commit();
            }
        }

        /// <summary>
        /// Abort current message
        /// </summary>
        public void Abort()
        {
            if (MessageQueueTransaction.Status == MessageQueueTransactionStatus.Pending)
            {
                MessageQueueTransaction.Abort();
            }
        }
    }
}