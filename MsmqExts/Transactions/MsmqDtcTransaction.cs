//using System;
//#if NET462
//using System.Messaging;
//#else
//using Experimental.System.Messaging;
//#endif
//using System.Transactions;

//namespace MsmqExts
//{
//    public class MsmqDtcTransaction : IMsmqTransaction
//    {
//        private readonly TransactionScope _scope;
//        private TransactionScope _suppressedScope;

//        public MsmqDtcTransaction()
//        {
//            _scope = new TransactionScope(TransactionScopeOption.Required, TimeSpan.Zero);
//        }

//        public void Dispose()
//        {
//            if (_suppressedScope != null)
//            {
//                _suppressedScope.Complete();
//                _suppressedScope.Dispose();
//            }

//            _scope.Dispose();
//        }

//        public void Commit()
//        {
//            _scope.Complete();
//        }

//        public void Abort()
//        {
//        }

//        public Message Receive(MessageQueue queue, TimeSpan timeout)
//        {
//            if (queue.Transactional)
//            {
//                var message = queue.Receive(timeout, MessageQueueTransactionType.Automatic);
//                _suppressedScope = new TransactionScope(TransactionScopeOption.Suppress, TimeSpan.Zero);

//                return message;
//            }
//            else
//            {
//                throw new Exception("MSMQ Queue must be transaction");
//            }
//        }
//    }
//}
