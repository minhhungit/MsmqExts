using MsmqExts;
using SimpleMessage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SimpleConsumerBatch
{
    class Program
    {
        static MsmqMessageQueue _messageQueue = new MsmqMessageQueue(".\\private$\\hungvo-hello",
            new MsmqMessageQueueSettings {
                ReceiveTimeout = TimeSpan.FromSeconds(1),
                LogDequeueElapsedTimeAction = (t) =>
                {
                    //Console.WriteLine($"picking message took {t.TotalMilliseconds}ms");
                }
            });

        static List<ProductMessage> _fakeDatabase = new List<ProductMessage>();
        static CancellationTokenSource _tokenSource = new CancellationTokenSource();
        static CancellationToken _token = _tokenSource.Token;

        static void Main(string[] args)
        {
            // press ctrl + C to cancel
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                Console.WriteLine("Cancel event triggered");
                _tokenSource.Cancel();
                eventArgs.Cancel = true;
            };

            Console.WriteLine("Starting...");

            // there settings should be in  app settings
            bool byPassIfError = false;
            int batchSize = 10000;
            TimeSpan outOfMessageDelayTime = TimeSpan.FromSeconds(10);

            Task.Run(() =>
            {
                while (true)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(5));
                    Console.WriteLine($"Total records in DB: { _fakeDatabase.Count}");
                }
            });
            
            while (true)
            {
                DequeueBatchResult batchDequeueResult = null;
                IFetchedMessage noHandlerMessage = null;

                bool hasNoHandlerMessage = false;

                try
                {
                    batchDequeueResult = _messageQueue.DequeueBatch(batchSize, _token);
                    
                    // throw exception if there is a BadMessage in batch
                    // this command should placed right after DequeueBatch(batchSize, token)
                    batchDequeueResult.ThrowIfHasAnBadMessage();

                    if (batchDequeueResult.NumberOfDequeuedMessages > 0)
                    {
                        Console.WriteLine($"Tried to fetch a batch {batchSize} messages (un-committed), got {batchDequeueResult.NumberOfDequeuedMessages}/{batchSize}, avg {Math.Round(batchDequeueResult.DequeueElapsed.TotalMilliseconds / batchDequeueResult.NumberOfDequeuedMessages, 2)}ms per message");
                    }
                    else
                    {
                        Console.WriteLine($"No more message in queue or timeout, will delay in {outOfMessageDelayTime.TotalSeconds} seconds to next fetching");
                        Thread.Sleep(outOfMessageDelayTime);
                        continue;
                    }

                    // product store
                    var tempProducts = new List<ProductMessage>();

                    // parsing
                    foreach (var fetchedMsg in batchDequeueResult.Messages)
                    {
                        var msgResult = _messageQueue.GetMessageResult(fetchedMsg);

                        if (msgResult is ProductMessage prod)
                        {
                            tempProducts.Add(prod);
                        }
                        else
                        {
                            noHandlerMessage = fetchedMsg;
                            hasNoHandlerMessage = true;
                            break;
                        }
                    }

                    // to simplify, if there is a message that has no handler, we will abort all messages in batch and throw exception to warning developer
                    // developer should add code to hanlde the message
                    // or deleting it by manual
                    if (hasNoHandlerMessage)
                    {
                        batchDequeueResult.Transaction.Abort();
                        batchDequeueResult.Transaction.Dispose();

                        throw new MsmqMessageHasNoHandlerException();
                    }
                    else // if all message are valid, we will process them, here is simple demo of a batch insert
                    {
                        if (tempProducts.Any())
                        {
                            // imagine this is a bulk insert into database
                            _fakeDatabase.AddRange(tempProducts);
                        }

                        // after inserting, we will commit messages
                        batchDequeueResult.Transaction.Commit();
                        batchDequeueResult.Transaction.Dispose();
                    }
                }
                catch (MsmqMessageHasNoHandlerException)
                {
                    Console.WriteLine($"Message has no handler, consumer will be stopped, please review your code, message label is [{noHandlerMessage.Label}]");
                    break;
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine($"Consusmer is cancelled.");
                    break;
                }
                catch(Exception ex)
                {
                    if (byPassIfError)
                    {
                        try
                        {
                            batchDequeueResult?.Transaction?.Commit();
                            batchDequeueResult?.Transaction?.Dispose();
                        }
                        catch { }

                        Console.WriteLine($"Got un-expected exception but it is ignored because [byPassIfError = {byPassIfError}], this is an informational message only, no user action is required.");
                    }
                    else
                    {
                        try
                        {
                            batchDequeueResult?.Transaction?.Abort();
                            batchDequeueResult?.Transaction?.Dispose();
                        }
                        catch { }

                        Console.WriteLine($"{ex.Message}\nConsumer will be stopped due to getting exception, [byPassIfError = {byPassIfError}]: {ex.Message}");
                        break;
                    }
                }
            }

            Console.WriteLine("Stopped");
            Console.ReadKey();
        }
    }
}
