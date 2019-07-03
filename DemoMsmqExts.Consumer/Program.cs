using DemoMsmqExts.Messages;
using MsmqExts;
using System;
using System.Collections.Generic;
using System.Threading;

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
            var batchSize = 5;

            var delayNoWorker = new TimeSpan(0, 0, 5);
            var exceptionDelay = new TimeSpan(0, 0, 10);
            var ignoreIfError = false;

            // show current number of messages on queue
            CountAndShowMessagesOnQueue(new string[] { queueName });


            Console.WriteLine("--------------------\n");

            var _jobQueue = new MsmqJobQueue(MsmqTransactionType.Internal);

            while (true)
            {
                var msg = new List<IFetchedJob>();

                try
                {
                    if (batchSize > 0)
                    {
                        if (mode == DemoFetchMode.OneMessage)
                        {
                            var deObj = _jobQueue.Dequeue(queueName, token);

                            if (deObj != null)
                            {
                                msg.Add(deObj);
                            }
                        }

                        if (mode == DemoFetchMode.BatchMessage)
                        {
                            msg = _jobQueue.DequeueList(queueName, batchSize, token);
                        }

                        if (msg.Count > 0)
                        {
                            foreach (var item in msg)
                            {
                                if (item.Result is Product prod)
                                {
                                    Console.WriteLine($"- processing product <{prod.Id}>");
                                }
                            }

                            foreach (var item in msg)
                            {
                                item.RemoveFromQueue();
                                item.Dispose();
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

                    foreach (var item in msg)
                    {
                        if (ignoreIfError)
                        {
                            item.RemoveFromQueue();
                        }
                        else
                        {
                            item.Requeue();
                        }

                        item.Dispose();
                    }

                    Thread.Sleep(exceptionDelay);
                }
                finally
                {
                    msg = new List<IFetchedJob>();
                }

                Console.WriteLine("- - - - - - - ");
            }
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
