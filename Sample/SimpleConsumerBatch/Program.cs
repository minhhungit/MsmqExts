using MsmqExts;
using SimpleMessage;
using System;
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
                        throw new Exception("Has Error: " + firstErrorMessage.DequeueException?.Message);
                    }
                    else
                    {
                        // import
                        var cleanMessages = messages.Where(x => x.DequeueResultStatus == DequeueResultStatus.Success).ToList();

                        foreach (var item in cleanMessages)
                        {
                            if (item.Result is ProductMessage prod)
                            {
                                var shortId = prod.Id.ToString().Substring(0, 7);
                                Console.WriteLine($"- processing product <{shortId}> - {prod.CreatedDate.ToString("HH:mm:ss.fff")}");

                                item?.Commit();
                                item?.Dispose();
                            }
                            else
                            {
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

                        // ThrowFailureMessage
                        if (firstErrorMessage != null && exceptionBehavior == DequeueExceptionBehavior.OnlyThrowFailureMessage)
                        {
                            throw new Exception("Has Error: " + firstErrorMessage.DequeueException?.Message);
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
