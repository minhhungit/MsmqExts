using MsmqExts;
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
        static MsmqJobQueue _jobQueue = new MsmqJobQueue();
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

                    Thread.Sleep(10);
                }
            });

            Console.WriteLine("fetching, please wait...");

            CancellationTokenSource tokenSource = new CancellationTokenSource();
            CancellationToken token = tokenSource.Token;

            Task.Run(() =>
            {
                while (true)
                {
                    using (var deObj = _jobQueue.Dequeue(QUEUE_NAME, token))
                    {
                        try
                        {
                            if (deObj.Result is MyMessage prod)
                            {
                                var shortId = prod.Id.ToString().Substring(0, 7);
                                Console.WriteLine($"- processing product <{shortId}> - {prod.CreatedDate.ToString("HH:mm:ss.fff")}");
                            }
                            deObj.Commit();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Error: {ex.Message}");

                            deObj.Abort();
                        }
                    }

                    Thread.Sleep(10);
                }
            });

            Console.ReadKey();
        }
    }
}
