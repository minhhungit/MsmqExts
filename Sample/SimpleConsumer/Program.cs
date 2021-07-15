using MsmqExts;
using SimpleMessage;
using System;
using System.Linq;
using System.Threading;

namespace SimpleConsumer
{
    class Program
    {
        static MsmqMessageQueue _messageQueue = new MsmqMessageQueue();
        const string QUEUE_NAME = ".\\private$\\hungvo-hello";

        static void Main(string[] args)
        {
            bool byPassIfError = true;

            Console.WriteLine("fetching, please wait...");

            CancellationTokenSource tokenSource = new CancellationTokenSource();
            CancellationToken token = tokenSource.Token;

            try
            {
                while (true)
                {
                    var message = _messageQueue.Dequeue(QUEUE_NAME, token);

                    switch (message.DequeueResultStatus)
                    {
                        case DequeueResultStatus.Success:
                            if (message.Result is ProductMessage prod)
                            {
                                var shortId = prod.Id.ToString().Substring(0, 7);
                                Console.WriteLine($"- processing product <{shortId}> - {prod.CreatedDate.ToString("HH:mm:ss.fff")}");

                                message?.Commit();
                                message?.Dispose();
                            }
                            else
                            {
                                if (byPassIfError)
                                {
                                    message?.Commit();
                                    message?.Dispose();
                                }
                                else
                                {
                                    // invaild message
                                    throw new Exception("Invaild message");
                                }
                            }
                            break;
                        case DequeueResultStatus.Timeout:
                            break;
                        case DequeueResultStatus.Exception:
                            throw new Exception("Has Error: " + message.DequeueException?.Message);
                        default:
                            break;
                    }

                    Thread.Sleep(TimeSpan.FromMilliseconds(1));
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
