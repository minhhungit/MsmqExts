using DemoMsmqExts.Messages;
using MsmqExts;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DemoMsmqExts.PublisherNetCore
{
    class Program
    {
        static void Main(string[] args)
        {
            CancellationTokenSource tokenSource = new CancellationTokenSource();
            CancellationToken token = tokenSource.Token;
            var queueName = AppConstants.MyQueueName;
            var delayNoWorker = new TimeSpan(0, 0, 5);

            var _jobQueue = new MsmqJobQueue(MsmqTransactionType.Internal);

            Task.Run(() =>
            {
                for (int number = 1; number <= 1000; number++)
                {
                    try
                    {
                        var obj = new Product
                        {
                            Id = number,
                            Name = @"product name " + number
                        };

                        _jobQueue.Enqueue(queueName, obj);

                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                    Console.WriteLine($"- created product <{number}>");
                }
            });

            Console.ReadKey();
        }
    }
}
