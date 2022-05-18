using MsmqExts;
using SimpleMessage;
using SimpleShare;
using System;
using System.Diagnostics;

namespace SimplePublisher
{
    class Program
    {
        static void Main(string[] args)
        {
            var useParallel = true;
            var parallelBatchSize = 10000;
            var textByteSize = 500;

            try
            {
                MsmqMessageQueue messageQueue = new MsmqMessageQueue(".\\private$\\hungvo-hello");

                while (true)
                {
                    var obj = new ProductMessage(Helper.GenerateString(textByteSize, messageQueue.Settings.Encoding));

                    Stopwatch sw = Stopwatch.StartNew();

                    if (useParallel)
                    {
                        System.Threading.Tasks.Parallel.For(0, parallelBatchSize, i =>
                        {
                            messageQueue.Enqueue(obj);
                        });
                    }
                    else
                    {
                        messageQueue.Enqueue(obj);
                    }

                    sw.Stop();

                    if (useParallel)
                    {
                        Console.WriteLine($"Enqueued {parallelBatchSize} messages(s) in {Math.Round(sw.Elapsed.TotalMilliseconds, 2)}ms, avg {Math.Round(sw.Elapsed.TotalMilliseconds / parallelBatchSize, 2)}ms per message");
                    }
                    else
                    {
                        Console.WriteLine($"Enqueued 1 message in {Math.Round(sw.Elapsed.TotalMilliseconds, 2)}ms");
                    }                    
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
