using MsmqExts;
using SimpleMessage;
using SimpleShare;
using System;
using System.Diagnostics;
using System.Linq;

namespace SimplePublisherBatch
{
    class Program
    {
        static void Main(string[] args)
        {
            var batchSize = 10000;
            int textByteSize = 500;

            try
            {
                MsmqMessageQueue messageQueue = new MsmqMessageQueue(".\\private$\\hungvo-hello");

                while (true)
                {
                    var productMessageBatch = Enumerable.Range(0, batchSize).Select(seq =>
                    {
                        return new ProductMessage(Helper.GenerateString(textByteSize, messageQueue.Settings.Encoding));
                    }).ToArray();

                    Stopwatch sw = Stopwatch.StartNew();

                    messageQueue.EnqueueBatch(productMessageBatch);

                    sw.Stop();

                    Console.WriteLine($"Enqueued a batch {batchSize} message(s) in {Math.Round(sw.Elapsed.TotalMilliseconds, 2)}ms, avg {Math.Round(sw.Elapsed.TotalMilliseconds / batchSize, 2)}ms per message");
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
