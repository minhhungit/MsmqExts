using MsmqExts;
using SimpleMessage;
using System;
using System.Diagnostics;
using System.Linq;

namespace SimplePublisherBatch
{
    class Program
    {
        static void Main(string[] args)
        {
            var batchSize = 50000;
            try
            {
                MsmqMessageQueue messageQueue = new MsmqMessageQueue(".\\private$\\hungvo-hello");

                while (true)
                {
                    Stopwatch sw = Stopwatch.StartNew();

                    var objs = Enumerable.Range(0, batchSize).Select(seq =>
                    {
                        return new ProductMessage(Guid.NewGuid(), DateTime.Now, seq);
                    }).ToArray();

                    messageQueue.EnqueueBatch(objs);

                    sw.Stop();

                    Console.WriteLine($"Enqueued a batch {batchSize} message(s) in {sw.Elapsed.TotalMilliseconds}ms, avg {Math.Round(sw.Elapsed.TotalMilliseconds / batchSize, 2)}ms per message");
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
