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
        ThrowImmediately = 1,
        OnlyThrowFailureMessage = 2,
        Continues = 4
    }

    class Program
    {
        static MsmqMessageQueue _messageQueue = new MsmqMessageQueue(new MsmqMessageQueueSettings { ReceiveTimeout = TimeSpan.FromSeconds(1), MessageOrder = false, DequeueWorkerCount = Environment.ProcessorCount * 1 });
        const string QUEUE_NAME = ".\\private$\\hungvo-hello";
        static List<ProductMessage> _fakeDatabase = new List<ProductMessage>();

        static void Main(string[] args)
        {
            var exceptionBehavior = DequeueExceptionBehavior.OnlyThrowFailureMessage;
            bool byPassIfError = true;
            TimeSpan outOfMessageDelay = TimeSpan.FromSeconds(30);

            Console.WriteLine("batch fetching, please wait...");

            CancellationTokenSource tokenSource = new CancellationTokenSource();
            CancellationToken token = tokenSource.Token;

            try
            {
                while (true)
                {
                    var messages = _messageQueue.DequeueList(QUEUE_NAME, 10, token);
                    var firstErrorMessage = messages.FirstOrDefault(x => x.DequeueResultStatus == DequeueResultStatus.Exception);

                    if (firstErrorMessage != null && exceptionBehavior == DequeueExceptionBehavior.ThrowImmediately)
                    {
                        throw firstErrorMessage.DequeueException;
                    }
                    else
                    {
                        //transaction store
                        var cleanMessages = messages.Where(x => x.DequeueResultStatus == DequeueResultStatus.Success).ToList();

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
                                if (byPassIfError)
                                {
                                    item?.Commit();
                                    item?.Dispose();
                                }
                                else
                                {
                                    // invaild message
                                    throw new Exception("Invaild message");
                                }
                            }
                        }

                        // demo bulk import
                        try
                        {
                            if (batchProducts.Any())
                            {
                                // should be in transaction, this is demo so imagine that we used transaction
                                _fakeDatabase.AddRange(batchProducts);
                            }
                            
                            Console.WriteLine($"Total records in DB: { _fakeDatabase.Count}");

                            // commit
                            foreach (var item in cleanMessages)
                            {
                                item?.Commit();
                                item?.Dispose();
                            }
                        }
                        catch (Exception ex)
                        {
                            // abort                            
                            foreach (var item in cleanMessages)
                            {
                                if (byPassIfError)
                                {
                                    item?.Commit();
                                }
                                else
                                {
                                    item?.Abort();
                                }

                                item?.Dispose();
                            }

                            // lastly, if we don't want to bypass error, throw exception
                            if (!byPassIfError)
                            {
                                throw ex;
                            }                            
                        }

                        // ThrowFailureMessage
                        if (firstErrorMessage != null && exceptionBehavior == DequeueExceptionBehavior.OnlyThrowFailureMessage)
                        {
                            throw firstErrorMessage.DequeueException;
                        }
                    }

                    // if has error or timeout then wait a bit
                    if (messages.Any(x => x.DequeueResultStatus != DequeueResultStatus.Success))
                    {
                        Thread.Sleep(outOfMessageDelay);
                    }

                    Thread.Sleep(10);
                    Console.WriteLine("--------------");
                }
            }
            catch (Exception ex)
            {
                if (!byPassIfError)
                {
                    Console.WriteLine($"{ex.Message}\nConsumer will be stopped !!!");
                    //throw ex;
                }
            }

            Console.ReadKey();
        }
    }
}
