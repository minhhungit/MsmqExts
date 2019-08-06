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
    public class MsmqJobQueueSettings
    {
        public MsmqTransactionType TransactionType { get; set; }
        public int? DequeueNbrOfTasks { get; set; }
        public TimeSpan? ReceiveTimeout { get; set; }
    }

    public class MsmqJobQueue
    {
        private readonly MsmqJobQueueSettings _settings = null;

        public MsmqJobQueue()
        {
            _settings = new MsmqJobQueueSettings
            {
                TransactionType = MsmqTransactionType.Internal,
                ReceiveTimeout = TimeSpan.FromSeconds(5),
                DequeueNbrOfTasks = 5
            };
        }

        public MsmqJobQueue(MsmqJobQueueSettings settings)
        {
            _settings = settings;

            _settings.ReceiveTimeout = _settings.ReceiveTimeout ?? TimeSpan.FromSeconds(5);
            _settings.DequeueNbrOfTasks = _settings.DequeueNbrOfTasks ?? 5;
        }

        public bool IsMatchType<T>(object obj) where T : class
        {
            return typeof(T) == obj.GetType();
        }

        /// <summary>
        /// Dequeue message
        /// </summary>
        /// <param name="queueName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public IFetchedJob Dequeue(string queueName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var transaction = CreateTransaction();

            try
            {
                using (var messageQueue = new MessageQueue(queueName))
                {
                    var message = transaction.Receive(messageQueue, (TimeSpan)_settings.ReceiveTimeout);

                    if (message != null && !string.IsNullOrWhiteSpace(message.Label))
                    {
                        using (var reader = new StreamReader(message.BodyStream))
                        {
                            var msgType = Type.GetType(message.Label);

                            if (msgType != null)
                            {
                                return new MsmqFetchedJob(transaction, reader.ReadToEnd(), msgType);
                            }
                            else
                            {
                                // return and developer will decide how to process invaild message
                                return new MsmqFetchedJob(transaction);
                            }
                        }
                    }
                }
            }
            catch (MessageQueueException ex) when (ex.MessageQueueErrorCode == MessageQueueErrorCode.IOTimeout)
            {
                
            }
            finally
            {
                
            }

            return null;
        }

        /// <summary>
        /// Dequeue a list message (for some cases we need to get a list messages to do a bulk insert)
        /// </summary>
        /// <param name="queueName">Queue name</param>
        /// <param name="nbrMessages">Number of messages you want to pick</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public List<IFetchedJob> DequeueList(string queueName, int nbrMessages, CancellationToken cancellationToken)
        {
            var result = new List<IFetchedJob>();
            var counter = nbrMessages;
            while (counter > 0)
            {
                var listOfTasks = new List<Task>();

                for (var i = 0; i < _settings.DequeueNbrOfTasks; i++)
                {
                    // Note that we start the Task here
                    listOfTasks.Add(Task.Run(() =>
                    {
                        result.Add(Dequeue(queueName, cancellationToken));
                    }));

                    counter--;

                    if (counter == 0)
                    {
                        break;
                    }
                }

                Task.WaitAll(listOfTasks.ToArray());                
            }

            return result;
        }

        /// <summary>
        /// Enqueue message
        /// </summary>
        /// <typeparam name="T">Type of message</typeparam>
        /// <param name="queueName">Queue name</param>
        /// <param name="obj">Message</param>
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
            switch (_settings.TransactionType)
            {
                case MsmqTransactionType.Internal:
                    return new MsmqInternalTransaction();
                //case MsmqTransactionType.Dtc:
                //    return new MsmqDtcTransaction();
            }

            throw new InvalidOperationException("Unknown MSMQ transaction type: " + _settings.TransactionType);
        }
    }
}
