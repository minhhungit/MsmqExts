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
using MsmqExts.Extensions;
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

        public bool IsMatchType<T>(object obj) where T : class
        {
            return typeof(T) == obj.GetType();
        }

        /// <summary>
        /// Dequeue message
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public IFetchedMessage Dequeue(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            Stopwatch stopwatch = Stopwatch.StartNew();
            MsmqFetchedMessage result = null;

            IMsmqTransaction transaction = CreateTransaction();

            try
            {
                var message = transaction.Receive(Queue, Settings.ReceiveTimeout);
                var messageBody = message.BodyStream.ReadFromJson(message.Label);

                result = new MsmqFetchedMessage(transaction, message.Label, messageBody, DequeueResultStatus.Success, null);
            }
            catch (MessageQueueException ex) when (ex.MessageQueueErrorCode == MessageQueueErrorCode.IOTimeout)
            {
                // we abort transaction here because timeout is not big problem, just retry later, user no need to do anything
                transaction.Abort();
                transaction.Dispose();

                result = new MsmqFetchedMessage(null, null, null, DequeueResultStatus.Timeout, ex);
            }
            catch(Exception ex)
            {
                // NOTE: we won't abort transaction here, we will let user decide what they want to do with error
                // this allow them abiliby ignoring error message by calling commit() for some cases

                //transaction.Abort();      // DON'T UN-COMMENT OUT THIS LINE
                //transaction.Dispose();    // DON'T UN-COMMENT OUT THIS LINE

                Settings?.LogExceptioAction?.Invoke(ex);

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
            var result = new DequeueBatchResult();

            var successMessages = new List<IFetchedMessage>();
            Stopwatch stopwatch = Stopwatch.StartNew();

            while (result.NumberOfDequeuedMessages < batchSize)
            {
                try
                {
                    IFetchedMessage msg = Dequeue(cancellationToken);
                    result.NumberOfDequeuedMessages++;

                    if (msg.DequeueResultStatus == DequeueResultStatus.Success)
                    {
                        successMessages.Add(msg);
                    }
                    else if (msg.DequeueResultStatus == DequeueResultStatus.Timeout)
                    {
                        // we won't throw exception if dequeue get timeout
                        // here transaction was disposed before
                        break;
                    }
                    else if (msg.DequeueResultStatus == DequeueResultStatus.Exception)
                    {
                        if (successMessages.Any())
                        {
                            // if we have some messages that dequeued successfully, then return them
                            // AND abort transaction for the last dequeued message (the error one)
                            // we will throw exception for error one in next batch fetching
                            msg?.AbortTransaction();
                            msg.Dispose();

                            break;
                        }
                        else
                        {
                            // if 'error' message is the first one of batch then we will THROW exception
                            // and let user decide what they want to do with exception <queue message>
                            // they will have ability to ignore it, by calling commit() for some cases

                            result.BadMessage = msg;
                            break;
                        }
                    }
                    else
                    {
                        throw new Exception("Un-handled dequeue result status");
                    }
                }
                catch (Exception ex)
                {
                    if (stopwatch.IsRunning)
                    {
                        stopwatch.Stop();
                    }

                    result.DequeueElapsed = stopwatch.Elapsed;

                    foreach (var msg in successMessages)
                    {
                        msg.AbortTransaction();
                        msg.Dispose();
                    }

                    // if has un-expected exception reset all data
                    successMessages = new List<IFetchedMessage>();
                    Settings?.LogDequeueBatchElapsedTime?.Invoke(stopwatch.Elapsed, result.NumberOfDequeuedMessages, batchSize);

                    throw ex;
                }
            }

            result.GoodMessages = successMessages;

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
                using (MemoryStream messageMemory = new MemoryStream(Encoding.Default.GetBytes(Newtonsoft.Json.JsonConvert.SerializeObject(obj))))
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
                Settings?.LogExceptioAction?.Invoke(ex);
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
    }
}
