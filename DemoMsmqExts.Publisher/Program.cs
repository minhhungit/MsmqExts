using DemoMsmqExts.Messages;
using MsmqExts;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace DemoMsmqExts.Publisher
{
    class Program
    {
        static void Main(string[] args)
        {
            var queueName = AppConstants.MyQueueName;
            var delayNoWorker = new TimeSpan(0, 0, 5);

            var _jobQueue = new MsmqJobQueue(MsmqTransactionType.Internal);

            Task.Run(() =>
            {
                for (int number = 1; number <= 1000; number++)
                {
                    var obj = new Product
                    {
                        Id = number,
                        Name = @"product name " + number
                    };

                    _jobQueue.Enqueue(queueName, obj);

                    Console.WriteLine($"- created product <{number}>");
                }
            });

            Console.ReadKey();
        }
    }
}
