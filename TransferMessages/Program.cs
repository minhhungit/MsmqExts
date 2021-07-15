using MsmqExts;
using SimpleMessage;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TransferMessages
{
    class Program
    {
        static MsmqMessageQueue _messageQueue = new MsmqMessageQueue();
        static string SOURCE_QUEUE_NAME = ConfigurationManager.AppSettings["QueueSourcePath"];
        static string TARGTE_QUEUE_NAME = ConfigurationManager.AppSettings["QueueTargetPath"];
        static int DELAY_TIME = int.Parse(ConfigurationManager.AppSettings["DelayTimeMilliseconds"]);

        static void Main(string[] args)
        {
            Console.WriteLine("fetching, please wait...");

            CancellationTokenSource tokenSource = new CancellationTokenSource();
            CancellationToken token = tokenSource.Token;

            try
            {
                var number = 0;
                while (true)
                {
                    number++;
                    var message = _messageQueue.Dequeue(SOURCE_QUEUE_NAME, token);

                    switch (message.DequeueResultStatus)
                    {
                        case DequeueResultStatus.Success:

                            _messageQueue.Enqueue(TARGTE_QUEUE_NAME, message.Label, message.Result);

                            message?.Commit();
                            message?.Dispose();

                            if (number > 100)
                            {
                                Console.WriteLine("running... " + DateTime.Now);
                                number = 0;
                            }

                            break;
                        case DequeueResultStatus.Timeout:
                            Console.WriteLine("TIMEOUT - MAYBE NO MORE MESSAGES ON QUEUE");
                            Thread.Sleep(TimeSpan.FromSeconds(10));
                            break;
                        case DequeueResultStatus.Exception:
                            throw new Exception("Has Error: " + message.DequeueException?.Message);
                        default:
                            break;
                    }

                    Thread.Sleep(TimeSpan.FromMilliseconds(DELAY_TIME));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}\nConsumer will be stopped !!!");
                throw ex;
            }

            Console.ReadKey();
        }
    }
}
