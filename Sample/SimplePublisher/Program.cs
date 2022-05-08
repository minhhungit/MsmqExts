using MsmqExts;
using Newtonsoft.Json;
using SimpleMessage;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace SimplePublisher
{
    class Program
    {
        static void Main(string[] args)
        {
            var batchSize = 10000;
            try
            {
                MsmqMessageQueue messageQueue = new MsmqMessageQueue(".\\private$\\hungvo-hello");

                while (true)
                {
                    Stopwatch sw = Stopwatch.StartNew();

                    Parallel.For(0, batchSize, i =>
                    {
                        var obj = new ProductMessage
                        {
                            Id = Guid.NewGuid(),
                            CreatedDate = DateTime.Now,
                            Seq = i
                        };

                        messageQueue.Enqueue(obj);
                    });

                    sw.Stop();

                    Console.WriteLine($"Sent {batchSize} in {sw.Elapsed.TotalMilliseconds}ms, avg {Math.Round(sw.Elapsed.TotalMilliseconds / batchSize, 2)}ms per message");
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
