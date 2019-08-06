using MsmqExts;
using Newtonsoft.Json;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleMsmqExtsSample
{
    public class MyMessage
    {
        public Guid Id { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    class Program
    {
        static MsmqJobQueue _jobQueue = new MsmqJobQueue(new MsmqJobQueueSettings {ReceiveTimeout = TimeSpan.FromSeconds(1), MessageOrder = false });
        const string QUEUE_NAME = ".\\private$\\hungvo-hello";

        static void Main(string[] args)
        {
            Task.Run(() =>
            {
                while (true)
                {
                    var id = Guid.NewGuid();
                    var obj = new MyMessage
                    {
                        Id = id,
                        CreatedDate = DateTime.Now
                    };

                    _jobQueue.Enqueue(QUEUE_NAME, obj);
                    Console.WriteLine("Enqueued: "+ JsonConvert.SerializeObject(obj));
                    Thread.Sleep(500);
                }
            });

            Console.WriteLine("fetching, please wait...");

            CancellationTokenSource tokenSource = new CancellationTokenSource();
            CancellationToken token = tokenSource.Token;

            Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        var deObjs = _jobQueue.DequeueList(QUEUE_NAME, 100, token);
                        foreach (var item in deObjs)
                        {
                            if (item == null)
                            {
                                continue;
                            }

                            if (item.Result == null)
                            {
                                continue;
                            }


                            try
                            {
                                if (item.Result is MyMessage prod)
                                {
                                    var shortId = prod.Id.ToString().Substring(0, 7);
                                    Console.WriteLine($"- processing product <{shortId}> - {prod.CreatedDate.ToString("HH:mm:ss.fff")}");
                                }
                                item.Commit();
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Error: {ex.Message}");

                                item.Abort();
                            }

                            item.Dispose();
                        }

                        Thread.Sleep(10);
                        Console.WriteLine(DateTime.Now);

                    }catch(Exception ex)
                    {

                    }
                }
            });

            Console.ReadKey();
        }
    }
}
