// Inspired by https://www.hangfire.io/

using System;
using System.Collections.Generic;
using System.IO;
#if NET462
using System.Messaging;
#else
using Experimental.System.Messaging;
#endif
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MsmqExts
{
    public class MsmqJobQueue
    {
        private readonly MsmqTransactionType _transactionType;
        private readonly TimeSpan ReceiveTimeout = TimeSpan.FromSeconds(5);
        public MsmqJobQueue(MsmqTransactionType transactionType)
        {
            _transactionType = transactionType;
        }

        public bool IsMatchType<T>(object obj) where T : class
        {
            return typeof(T) == obj.GetType();
        }

        public IFetchedJob Dequeue(string queueName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var transaction = CreateTransaction();

            try
            {
                using (var messageQueue = new MessageQueue(queueName))
                {
                    var message = transaction.Receive(messageQueue, ReceiveTimeout);

                    using (var reader = new StreamReader(message.BodyStream))
                    {
                        return new MsmqFetchedJob(transaction, reader.ReadToEnd(), Type.GetType(message.Label));
                    }
                }
            }
            catch (MessageQueueException ex) when (ex.MessageQueueErrorCode == MessageQueueErrorCode.IOTimeout)
            {
                return null;
            }
            finally
            {
                
            }
        }

        public List<IFetchedJob> DequeueList(string queueName, int batchSize, CancellationToken cancellationToken)
        {
            var result = new List<IFetchedJob>();

            var listOfTasks = new List<Task>();

            for (var i = 0; i < batchSize; i++)
            {
                // Note that we start the Task here
                listOfTasks.Add(Task.Run(() =>
                {
                    var deObj = Dequeue(queueName, cancellationToken);

                    if (deObj != null)
                    {
                        result.Add(deObj);
                    }
                }));
            }

            Task.WaitAll(listOfTasks.ToArray());

            return result;
        }

        public void Enqueue<T>(string queueName, T obj)
        {
            using (var messageQueue = new MessageQueue(queueName))
            using (MemoryStream messageMemory = new MemoryStream(Encoding.Default.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(obj))))
            {
                using (var message = new Message
                {
                    BodyStream = messageMemory,
                    Label = obj.GetType().AssemblyQualifiedName,
                    Recoverable = true,
                    UseDeadLetterQueue = true
                })
                using (var transaction = new MessageQueueTransaction())
                {
                    transaction.Begin();
                    messageQueue.Send(message, transaction);
                    transaction.Commit();
                }
            }            
        }

        private IMsmqTransaction CreateTransaction()
        {
            switch (_transactionType)
            {
                case MsmqTransactionType.Internal:
                    return new MsmqInternalTransaction();
                //case MsmqTransactionType.Dtc:
                //    return new MsmqDtcTransaction();
            }

            throw new InvalidOperationException("Unknown MSMQ transaction type: " + _transactionType);
        }
    }
}
