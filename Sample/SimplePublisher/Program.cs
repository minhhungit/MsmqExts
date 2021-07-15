using MsmqExts;
using Newtonsoft.Json;
using SimpleMessage;
using System;
using System.Threading;

namespace SimplePublisher
{
    class Program
    {
        static MsmqMessageQueue _messageQueue = new MsmqMessageQueue();
        const string QUEUE_NAME = ".\\private$\\hungvo-hello";

        static void Main(string[] args)
        {
            try
            {
                while (true)
                {
                    var id = Guid.NewGuid();
                    var obj = new ProductMessage
                    {
                        Id = id,
                        CreatedDate = DateTime.Now
                    };

                    Console.WriteLine("Enqueuing: " + JsonConvert.SerializeObject(obj));

                    _messageQueue.Enqueue(QUEUE_NAME, obj);
                    Thread.Sleep(TimeSpan.FromMilliseconds(1));
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            Console.ReadKey();
        }
    }
}
