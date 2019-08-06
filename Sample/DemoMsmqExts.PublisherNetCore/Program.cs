﻿using DemoMsmqExts.Messages;
using MsmqExts;
using System;
using System.Threading.Tasks;

namespace DemoMsmqExts.PublisherNetCore
{
    class Program
    {
        static void Main(string[] args)
        {
            var queueName = AppConstants.MyQueueName;

            var _jobQueue = new MsmqJobQueue();

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