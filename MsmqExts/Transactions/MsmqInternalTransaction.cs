// Inspired by https://www.hangfire.io/

using System;
using System.Messaging;

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

        public Message Receive(MessageQueue queue, TimeSpan timeout)
        {
            if (queue.Transactional)
            {
                _transaction.Begin();
                return queue.Receive(timeout, _transaction);
            }
            else
            {
                throw new Exception("MSMQ Queue must be transaction");
            }     
        }

        public void Commit()
        {
            _transaction.Commit();
        }

        public void Abort()
        {
            _transaction.Abort();
        }
    }
}
