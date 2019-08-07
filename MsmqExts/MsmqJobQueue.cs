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
using System.Collections.Concurrent;
using MsmqExts.Extensions;

namespace MsmqExts
{
    public class MsmqMessageQueueSettings
    {
        public MsmqTransactionType TransactionType { get; set; }
        public TimeSpan? ReceiveTimeout { get; set; }

        /// <summary>
        /// Make sure that message will be picked by ordering
        /// Default value = true (with order)
        /// If value = null or false, MsmqExts will pick messages using multi tasks for faster
        /// </summary>
        public bool? MessageOrder { get; set; }

        /// <summary>
        /// Dequeue worker count, this setting is used when MessageOrder = false or null
        /// Default value = Environment.ProcessorCount * 5
        /// </summary>
        public int? DequeueWorkerCount { get; set; }

        /// <summary>
        /// Log callback method
        /// </summary>
        public Action<Exception> LogAction { get; set; }
    }

    public class MsmqMessageQueue
    {
        private readonly MsmqMessageQueueSettings _settings = null;
        private static TimeSpan _receiveTimeoutDefault = TimeSpan.FromSeconds(2);
        private static bool _messageOrder = true;
        private static int _dequeueWorkerCount = Environment.ProcessorCount * 5;
        public Action<Exception> LogAction { get; set; }

        public MsmqMessageQueue()
        {
            _settings = new MsmqMessageQueueSettings
            {
                TransactionType = MsmqTransactionType.Internal,
                ReceiveTimeout = _receiveTimeoutDefault,
                MessageOrder = _messageOrder,
                DequeueWorkerCount = _dequeueWorkerCount
            };
        }

        public MsmqMessageQueue(MsmqMessageQueueSettings settings)
        {
            _settings = settings;

            _settings.ReceiveTimeout = _settings.ReceiveTimeout ?? _receiveTimeoutDefault;
            _settings.MessageOrder = _settings.MessageOrder ?? _messageOrder;
            _settings.DequeueWorkerCount = _settings.DequeueWorkerCount ?? _dequeueWorkerCount;
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
        public IFetchedMessage Dequeue(string queueName, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var transaction = CreateTransaction();

            try
            {
                using (var messageQueue = new MessageQueue(queueName))
                {
                    var message = transaction.Receive(messageQueue, (TimeSpan)_settings.ReceiveTimeout);
                    var messageBody = message.BodyStream.ReadFromJson(message.Label);

                    return new MsmqFetchedMessage(transaction, messageBody, DequeueResultStatus.Success, null);
                }
            }
            catch (MessageQueueException ex) when (ex.MessageQueueErrorCode == MessageQueueErrorCode.IOTimeout)
            {
                transaction.Abort();
                transaction.Dispose();

                return new MsmqFetchedMessage(null, null, DequeueResultStatus.Timeout, null);
            }
            catch(Exception ex)
            {
                transaction.Abort();
                transaction.Dispose();

                LogAction?.Invoke(ex);

                return new MsmqFetchedMessage(null, null, DequeueResultStatus.Exception, ex);
            }            
        }

        /// <summary>
        /// Dequeue a list message (for some cases we need to get a list messages to do a bulk insert)
        /// </summary>
        /// <param name="queueName">Queue name</param>
        /// <param name="nbrMessages">Number of messages you want to pick</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public List<IFetchedMessage> DequeueList(string queueName, int nbrMessages, CancellationToken cancellationToken)
        {
            if (_settings.MessageOrder ?? false)
            {
                var result = new List<IFetchedMessage>();
                for (var i = 0; i < nbrMessages; i++)
                {
                    var msg = Dequeue(queueName, cancellationToken);
                    if (msg.DequeueResultStatus == DequeueResultStatus.Success)
                    {
                        result.Add(msg);
                    }
                    else
                    {
                        // if message is timeout then return
                        if (msg.DequeueResultStatus == DequeueResultStatus.Timeout)
                        {
                            break;
                        }
                        else
                        {
                            throw msg.DequeueException ?? new Exception("Has error when dequeue message in queue");
                        }
                    }
                }

                return result;
            }
            else
            {
                var msgQueue = new ConcurrentBag<IFetchedMessage>();
                var counter = nbrMessages;
                while (counter > 0)
                {
                    var listOfTasks = new List<Task>();

                    for (var i = 0; i < _settings.DequeueWorkerCount; i++)
                    {
                        // Note that we start the Task here
                        listOfTasks.Add(Task.Run(() =>
                        {
                            msgQueue.Add(Dequeue(queueName, cancellationToken));
                        }));

                        counter--;

                        if (counter == 0)
                        {
                            break;
                        }
                    }

                    Task.WaitAll(listOfTasks.ToArray());
                }

                var result = new List<IFetchedMessage>();

                while (!msgQueue.IsEmpty)
                {
                    if (msgQueue.TryTake(out IFetchedMessage msg))
                    {
                        result.Add(msg);
                    }

                    Thread.Sleep(1);
                }

                return result;
            }
        }

        /// <summary>
        /// Enqueue message
        /// </summary>
        /// <typeparam name="T">Type of message</typeparam>
        /// <param name="queueName">Queue name</param>
        /// <param name="obj">Message</param>
        public void Enqueue<T>(string queueName, T obj)
        {
            try
            {
                using (var messageQueue = new MessageQueue(queueName))
                using (MemoryStream messageMemory = new MemoryStream(Encoding.Default.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(obj))))
                {
                    using (var message = new Message
                    {
                        BodyStream = messageMemory,
                        Label = obj.GetType().AssemblyQualifiedName,
                        Recoverable = true,
                        UseDeadLetterQueue = true,
                    })
                    using (var transaction = new MessageQueueTransaction())
                    {
                        transaction.Begin();
                        messageQueue.Send(message, transaction);
                        transaction.Commit();
                    }
                }
            }
            catch (Exception ex)
            {
                LogAction?.Invoke(ex);
                throw;
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
