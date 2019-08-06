using DemoMsmqExts.Messages;
using MsmqExts;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DemoMsmqExts.Consumer
{
    enum DemoFetchMode
    {
        OneMessage,
        BatchMessage
    }

    class Program
    {
        static void Main(string[] args)
        {
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            CancellationToken token = tokenSource.Token;

            var queueName = AppConstants.MyQueueName;

            DemoFetchMode mode = DemoFetchMode.BatchMessage;
            var batchSize = 50;

            var delayNoWorker = new TimeSpan(0, 0, 5);
            var exceptionDelay = new TimeSpan(0, 0, 10);
            var ignoreIfError = false;

            // show current number of messages on queue
            CountAndShowMessagesOnQueue(new string[] { queueName });

            Console.WriteLine("--------------------\n");

            var _jobQueue = new MsmqJobQueue(new MsmqJobQueueSettings {
                TransactionType = MsmqTransactionType.Internal,
                ReceiveTimeout = TimeSpan.FromSeconds(3),
                TaskBatchSize = 10
            });

            Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    // ConcurrentBag: Thread-safe implementation of an unordered collection of elements.
                    // so you have to keep an eye on ordering of messages, or use List<IFetchedJob>
                    var msgStore = new ConcurrentBag<IFetchedJob>();

                    try
                    {
                        if (batchSize > 0)
                        {
                            switch (mode)
                            {
                                case DemoFetchMode.OneMessage:
                                    var deObj = _jobQueue.Dequeue(queueName, token);

                                    if (deObj != null)
                                    {
                                        msgStore.Add(deObj);
                                    }

                                    break;
                                case DemoFetchMode.BatchMessage:
                                    var msgPkg = _jobQueue.DequeueList(queueName, batchSize, token);

                                    for (int i = 0; i < msgPkg.Count; i++)
                                    {
                                        if (msgPkg[i] != null)
                                        {
                                            msgStore.Add(msgPkg[i]);
                                        }
                                    }

                                    break;
                                default:
                                    break;
                            }

                            // make sure that we will filter out NULL messages
                            // NULL message means there is problem when dequeue message from queue (timeout maybe), 
                            // therefore, we will retry it in next turn
                            var transMsgStore = msgStore.Where(x => x != null).ToList();

                            if (transMsgStore.Count > 0)
                            {
                                foreach (var item in transMsgStore)
                                {
                                    if (item == null)
                                    {
                                        continue;
                                    }

                                    if (item.Result == null)
                                    {
                                        Console.WriteLine("[bad message]");
                                        continue;
                                    }

                                    if (item.Result is Product prod)
                                    {
                                        Console.WriteLine($"- processing product <{prod.Id}>");
                                    }
                                }

                                foreach (var item in transMsgStore)
                                {
                                    item?.RemoveFromQueue();
                                    item?.Dispose();
                                }
                            }
                            else
                            {
                                Console.WriteLine("No msg, waiting...");
                            }
                        }
                        else
                        {
                            Thread.Sleep(delayNoWorker);
                        }
                    }
                    catch (Exception ex)
                    {
                        // log exception here
                        // ErrorStore.LogException(ex);
                        Console.WriteLine(ex.Message);

                        foreach (var item in msgStore)
                        {
                            if (ignoreIfError)
                            {
                                item?.RemoveFromQueue();
                            }
                            else
                            {
                                item?.Requeue();
                            }

                            item?.Dispose();
                        }

                        Thread.Sleep(exceptionDelay);
                    }
                    finally
                    {
                        msgStore = new ConcurrentBag<IFetchedJob>();
                    }

                    Console.WriteLine("- - - - - - - ");
                }
            }, TaskCreationOptions.LongRunning).ConfigureAwait(false);

            Console.ReadKey();
        }

        static void CountAndShowMessagesOnQueue(string[] queueNames)
        {
            Console.WriteLine("Number of messages on queue:");

            for (int i = 0; i < queueNames.Length; i++)
            {
                //var idx = queueNames[i].ToUpper().IndexOf("PRIVATE$");

                var number = MessageQueueExtensions.GetCount(new System.Messaging.MessageQueue
                {
                    QueueName = queueNames[i].Substring(2)
                });

                Console.WriteLine($"- Queue [{queueNames[i].ToUpper()}]: {number} message(s)");
            }
        }
    }
}
