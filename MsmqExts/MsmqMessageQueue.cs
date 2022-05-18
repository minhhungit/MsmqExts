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
using System.Diagnostics;
using System.Linq;

namespace MsmqExts
{
    public class MsmqMessageQueue
    {
        public MessageQueue Queue { get; private set; }
        public MsmqMessageQueueSettings Settings { get; private set; }

        public MsmqMessageQueue(string queueName)
        {
            Queue = new MessageQueue(queueName);
            Settings = new MsmqMessageQueueSettings();
        }

        public MsmqMessageQueue(string queueName, MsmqMessageQueueSettings settings)
        {
            Queue = new MessageQueue(queueName);
            Settings = settings ?? new MsmqMessageQueueSettings();

            if (Settings.ReceiveTimeout == null)
            {
                Settings.ReceiveTimeout = TimeSpan.FromSeconds(2);
            }
        }

        public MsmqMessageQueue(Func<MessageQueue> queueFactory, MsmqMessageQueueSettings settings)
        {
            if (queueFactory == null)
            {
                throw new ArgumentNullException(nameof(queueFactory));  
            }

            Queue = queueFactory?.Invoke();

            Settings = settings ?? new MsmqMessageQueueSettings();

            if (Settings.ReceiveTimeout == null)
            {
                Settings.ReceiveTimeout = TimeSpan.FromSeconds(2);
            }
        }

        public bool IsMatchType<T>(object obj) where T : class
        {
            return typeof(T) == obj.GetType();
        }

        /// <summary>
        /// Dequeue message
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public IFetchedMessage Dequeue(CancellationToken cancellationToken, IMsmqTransaction msmqTransaction = null)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Stopwatch stopwatch = Stopwatch.StartNew();
            MsmqFetchedMessage result = null;

            IMsmqTransaction transaction;

            if (msmqTransaction == null)
            {
                transaction = CreateTransaction();
            }
            else
            {
                transaction = msmqTransaction;
            }
            
            try
            {
                var message = transaction.Receive(Queue, Settings.ReceiveTimeout);                
                result = new MsmqFetchedMessage(transaction, message.Label, message, DequeueResultStatus.Success, null);
            }
            catch (MessageQueueException ex) when (ex.MessageQueueErrorCode == MessageQueueErrorCode.IOTimeout)
            {
                // we abort transaction here because timeout is not big problem, just retry later, user no need to do anything
                // but only abort transaction if transaction is created by single receive
                // for batch receive (msmqTransaction != null) we won't abort
                if (msmqTransaction == null)
                {
                    transaction.Abort();
                    transaction.Dispose();
                }                

                result = new MsmqFetchedMessage(null, null, null, DequeueResultStatus.Timeout, ex);
            }
            catch(Exception ex)
            {
                result = new MsmqFetchedMessage(transaction, null, null, DequeueResultStatus.Exception, ex);
            }
            finally
            {
                if (stopwatch.IsRunning)
                {
                    stopwatch.Stop();
                }
                
                Settings?.LogDequeueElapsedTimeAction?.Invoke(stopwatch.Elapsed);
            }

            result.SetDequeueElapsed(stopwatch.Elapsed);

            return result;
        }

        /// <summary>
        /// Dequeue a list of messages (for some cases we need to get a list messages to do a bulk insert)
        /// </summary>
        /// <param name="batchSize">Number of messages you want to pick</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public DequeueBatchResult DequeueBatch(int batchSize, CancellationToken cancellationToken)
        {
            IMsmqTransaction sharedTransaction = CreateTransaction();
            var result = new DequeueBatchResult(sharedTransaction);
            Stopwatch stopwatch = Stopwatch.StartNew();

            for (int i = 0; i < batchSize; i++)
            {
                try
                {
                    IFetchedMessage msg = Dequeue(cancellationToken, sharedTransaction);
                    if (msg != null)
                    {
                        if (msg.DequeueResultStatus == DequeueResultStatus.Success)
                        {
                            result.Messages.Add(msg);
                            result.NumberOfDequeuedMessages++;
                        }
                        else if (msg.DequeueResultStatus == DequeueResultStatus.Timeout)
                        {
                            // we won't throw exception if dequeue get timeout
                            // here transaction was disposed before (if single receive)
                            break;
                        }
                        else if (msg.DequeueResultStatus == DequeueResultStatus.Exception)
                        {
                            if (result.NumberOfDequeuedMessages > 0)
                            {
                                // revert and re-fetch messages with new batchSize = NumberOf_SUCCESS_DequeuedMessages
                                // that mean we will fetch messages that placed in before this bad exception message
                                sharedTransaction.Abort();
                                sharedTransaction.Dispose();

                                if (stopwatch.IsRunning)
                                {
                                    stopwatch.Stop();
                                }
                                return DequeueBatch(result.NumberOfDequeuedMessages, cancellationToken);
                            }
                            else
                            {
                                result.Messages.Add(msg);
                                result.NumberOfDequeuedMessages++;
                                break;
                            }
                        }
                        else
                        {
                            throw new Exception($"Un-handled case DequeueResultStatus = {msg.DequeueResultStatus}");
                        }
                    }
                    else
                    {
                        break;
                    }
                }
                catch (Exception ex)
                {
                    // we normally should have NO any exception here
                    // for safe, we will abort transaction if have
                    // create me an issue on github to let me know, thank you
                    // here is github link: https://github.com/minhhungit/MsmqExts
                    sharedTransaction.Abort();
                    sharedTransaction.Dispose();

                    if (stopwatch.IsRunning)
                    {
                        stopwatch.Stop();
                    }
                    result.DequeueElapsed = stopwatch.Elapsed;
                    Settings?.LogDequeueBatchElapsedTime?.Invoke(stopwatch.Elapsed, result.NumberOfDequeuedMessages, batchSize);

                    throw ex;
                }
            }
            
            if (stopwatch.IsRunning)
            {
                stopwatch.Stop();
            }
            result.DequeueElapsed = stopwatch.Elapsed;
            Settings?.LogDequeueBatchElapsedTime?.Invoke(stopwatch.Elapsed, result.NumberOfDequeuedMessages, batchSize);

            return result;
        }

        /// <summary>
        /// Enqueue message
        /// </summary>
        /// <typeparam name="T">Type of message</typeparam>
        /// <param name="label">Message label</param>
        /// <param name="obj">Message</param>
        public void Enqueue<T>(string label, T obj)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            try
            {
                using (MemoryStream messageMemory = new MemoryStream(Settings.Encoding.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(obj))))
                {
                    using (var message = new Message
                    {
                        BodyStream = messageMemory,
                        Label = label,
                        Recoverable = true,
                        UseDeadLetterQueue = true,
                    })
                    using (var transaction = new MessageQueueTransaction())
                    {
                        transaction.Begin();
                        Queue.Send(message, transaction);
                        transaction.Commit();
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (stopwatch.IsRunning)
                {
                    stopwatch.Stop();
                }
                
                Settings?.LogEnqueueElapsedTimeAction?.Invoke(stopwatch.Elapsed);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T">Type of message</typeparam>
        /// <param name="obj">Message</param>
        public void Enqueue<T>(T obj)
        {
            Enqueue(obj.GetType().AssemblyQualifiedName, obj);
        }

        public void EnqueueBatch<T>(IEnumerable<T> objs, Func<T, string> labelFactory = null)
        {
            long batchSize = objs == null ? 0 : objs.LongCount();

            Stopwatch stopwatch = Stopwatch.StartNew();

            try
            {
                var transaction = new MessageQueueTransaction();
                foreach (var obj in objs ?? new List<T>())
                {
                    var label = string.Empty;
                    if (labelFactory != null)
                    {
                        label = labelFactory?.Invoke(obj);
                    }
                    else
                    {
                        label = obj.GetType().AssemblyQualifiedName;
                    }

                    if (transaction.Status == MessageQueueTransactionStatus.Initialized)
                    {
                        transaction.Begin();
                    }
                    
                    using (MemoryStream messageMemory = new MemoryStream(Settings.Encoding.GetBytes(Settings.SerializerHelper.SerializeObject(obj))))
                    {
                        using (var message = new Message
                        {
                            BodyStream = messageMemory,
                            Label = label,
                            Recoverable = true,
                            UseDeadLetterQueue = true,
                        })
                        {
                            Queue.Send(message, transaction);
                        }
                    }
                }

                if (transaction.Status == MessageQueueTransactionStatus.Pending)
                {
                    transaction.Commit();
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                if (stopwatch.IsRunning)
                {
                    stopwatch.Stop();
                }

                Settings?.LogEnqueueElapsedTimeAction?.Invoke(stopwatch.Elapsed);
            }
        }

        private IMsmqTransaction CreateTransaction()
        {
            switch (Settings.TransactionType)
            {
                case MsmqTransactionType.Internal:
                    return new MsmqInternalTransaction();
                //case MsmqTransactionType.Dtc:
                //    return new MsmqDtcTransaction();
            }

            throw new InvalidOperationException("Unknown MSMQ transaction type: " + Settings.TransactionType);
        }

        public object GetMessageResult(IFetchedMessage fetchedMessage)
        {
            return Settings.SerializerHelper.DeserializeObject(fetchedMessage.MsmqMessage.BodyStream, fetchedMessage.MsmqMessage.Label, Settings.Encoding);
        }
    }
}
