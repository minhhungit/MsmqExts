using MsmqExts;
using SimpleMessage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace SimpleConsumerBatch
{
    public enum DequeueExceptionBehavior
    {
        // throw exception immediately if see any EX (Exception) message
        ThrowImmediately = 1,

        // if has EX message, firstly process success messages then throw Exception after success messages are processed
        ThrowAfterProcessSuccessMessages = 2,

        // continues even there is EX message
        Continues = 4
    }

    class Program
    {
        static MsmqMessageQueue _messageQueue = new MsmqMessageQueue(new MsmqMessageQueueSettings { ReceiveTimeout = TimeSpan.FromSeconds(1), MessageOrder = false, DequeueWorkerCount = Environment.ProcessorCount * 1 });
        const string QUEUE_NAME = ".\\private$\\hungvo-hello";
        static List<ProductMessage> _fakeDatabase = new List<ProductMessage>();
        static CancellationTokenSource _tokenSource = new CancellationTokenSource();
        static CancellationToken _token = _tokenSource.Token;

        static void Main(string[] args)
        {
            // there settings should be in  app settings
            var exceptionBehavior = DequeueExceptionBehavior.ThrowAfterProcessSuccessMessages;
            bool byPassIfError = true;
            TimeSpan outOfMessageDelay = TimeSpan.FromSeconds(30);

            Console.WriteLine("batch fetching, please wait...");
            Console.WriteLine("------------------------------");

            var cleanMessages = new List<IFetchedMessage>();

            while (true)
            {
                var canBeOutOfMessages = false;
                try
                {
                    // make sure that we cleaned up store
                    cleanMessages = new List<IFetchedMessage>();

                    var messages = _messageQueue.DequeueList(QUEUE_NAME, 10, _token);
                    canBeOutOfMessages = messages.Any(x => x.DequeueResultStatus != DequeueResultStatus.Success);
                    var firstErrorMessage = messages.FirstOrDefault(x => x.DequeueResultStatus == DequeueResultStatus.Exception);

                    if (firstErrorMessage != null && exceptionBehavior == DequeueExceptionBehavior.ThrowImmediately)
                    {
                        throw firstErrorMessage.DequeueException;
                    }
                    else
                    {
                        //transaction store
                        cleanMessages = messages.Where(x => x.DequeueResultStatus == DequeueResultStatus.Success).ToList();

                        // product store
                        var batchProducts = new List<ProductMessage>();

                        // parse
                        foreach (var item in cleanMessages)
                        {
                            if (item.Result is ProductMessage prod)
                            {
                                batchProducts.Add(prod);
                            }
                            else
                            {
                                // invaild message (which did not match any type)
                                throw new Exception("Invaild message");
                            }
                        }

                        #region This block should be in transaction

                        // demo bulk import
                        if (batchProducts.Any())
                        {
                            _fakeDatabase.AddRange(batchProducts);
                        }

                        // commit
                        foreach (var msg in cleanMessages)
                        {
                            msg?.Commit();
                            msg.Dispose();
                        }

                        Console.WriteLine($"Total records in DB: { _fakeDatabase.Count}");

                        #endregion

                        // ThrowFailureMessage
                        if (firstErrorMessage != null && exceptionBehavior == DequeueExceptionBehavior.ThrowAfterProcessSuccessMessages)
                        {
                            throw firstErrorMessage.DequeueException;
                        }
                    }

                    Console.WriteLine("-------O-K-------");
                }
                catch(Exception ex)
                {
                    foreach (var msg in cleanMessages)
                    {
                        try
                        {
                            if (byPassIfError)
                            {
                                msg?.Commit();
                            }
                            else
                            {
                                msg?.Abort();
                            }

                            msg?.Dispose();
                        }
                        catch { }
                    }

                    if (!byPassIfError)
                    {
                        Console.WriteLine($"{ex.Message}\nConsumer will be stopped !!!");
                        break;
                    }
                }

                // if has error or timeout then wait a bit
                if (canBeOutOfMessages)
                {
                    Thread.Sleep(outOfMessageDelay);
                }

                Thread.Sleep(5);
            }

            Console.WriteLine("stopped");
            Console.ReadKey();
        }
    }
}
