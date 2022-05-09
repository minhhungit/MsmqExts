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
        private readonly MessageQueueTransaction _transaction;

        public MsmqInternalTransaction()
        {
            _transaction = new MessageQueueTransaction();
        }

        public void Dispose()
        {
            _transaction.Dispose();
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
                if (_transaction.Status == MessageQueueTransactionStatus.Initialized)
                {
                    _transaction.Begin();
                }
                
                return queue.Receive(timeout, _transaction);
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
            if (_transaction.Status == MessageQueueTransactionStatus.Pending)
            {
                _transaction.Commit();
            }
        }

        /// <summary>
        /// Abort current message
        /// </summary>
        public void Abort()
        {
            if (_transaction.Status == MessageQueueTransactionStatus.Pending)
            {
                _transaction.Abort();
            }
        }
    }
}