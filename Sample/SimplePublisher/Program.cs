using MsmqExts;
using SimpleMessage;
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

            try
            {
                MsmqMessageQueue messageQueue = new MsmqMessageQueue(".\\private$\\hungvo-hello");

                while (true)
                {
                    Stopwatch sw = Stopwatch.StartNew();

                    if (useParallel)
                    {
                        System.Threading.Tasks.Parallel.For(0, parallelBatchSize, i =>
                        {
                            var obj = new ProductMessage(Guid.NewGuid(), DateTime.Now, i);
                            messageQueue.Enqueue(obj);
                        });
                    }
                    else
                    {
                        var obj = new ProductMessage(Guid.NewGuid(), DateTime.Now, 0);
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
