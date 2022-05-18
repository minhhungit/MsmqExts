using MsmqExts;
using SimpleMessage;
using System;
using System.Threading;

namespace SimpleConsumer
{
    class Program
    {
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

            var msmqMessageQueue = new MsmqMessageQueue(".\\private$\\hungvo-hello");

            bool byPassIfError = false;
            bool ignoreMessageIfHasNoHandler = false;
            TimeSpan outOfMessageDelayTime = TimeSpan.FromSeconds(10);

            Console.WriteLine("fetching, please wait...");

            while (true)
            {
                IFetchedMessage fetchedMessage = null;
                string messageLabel = string.Empty;

                try
                {
                    fetchedMessage = msmqMessageQueue.Dequeue(_token);
                    messageLabel = fetchedMessage.Label;

                    switch (fetchedMessage.DequeueResultStatus)
                    {
                        case DequeueResultStatus.Success:
                            var messageResult = msmqMessageQueue.GetMessageResult(fetchedMessage);

                            if (messageResult is ProductMessage prod)
                            {
                                //var shortId = prod.Id.ToString().Substring(0, 7);
                                Console.WriteLine($"Got a product in {Math.Round(fetchedMessage.DequeueElapsed.TotalMilliseconds, 2)}ms");

                                fetchedMessage?.CommitTransaction();
                                fetchedMessage?.Dispose();
                            }
                            else
                            {
                                throw new MsmqMessageHasNoHandlerException();
                            }
                            break;
                        case DequeueResultStatus.Timeout:
                            Console.WriteLine($"Dequeue got timeout ({msmqMessageQueue.Settings.ReceiveTimeout.TotalSeconds} seconds), and will be delayed in {outOfMessageDelayTime.TotalSeconds} sec to next dequeue. This is an informational message only, no user action is required.");
                            Thread.Sleep(outOfMessageDelayTime);
                            break;
                        case DequeueResultStatus.Exception:
                            throw new Exception("Dequeue message got error: " + fetchedMessage.DequeueException?.Message);
                        default:
                            break;
                    }

                }
                catch (MsmqMessageHasNoHandlerException)
                {
                    if (ignoreMessageIfHasNoHandler)
                    {
                        fetchedMessage?.CommitTransaction();
                        Console.WriteLine($"Message has no handler, but was ignored [ignoreMessageIfHasNoHandler={ignoreMessageIfHasNoHandler}], message label is {messageLabel}, this is an informational message only, no user action is required.");
                        fetchedMessage?.Dispose();
                    }
                    else
                    {
                        fetchedMessage?.AbortTransaction();                        
                        Console.WriteLine($"Message has no handler, message label is {messageLabel}");
                        fetchedMessage?.Dispose();

                        break;
                    }
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine($"Consusmer is cancelled.");
                    break;
                }
                catch (Exception ex)
                {
                    if (byPassIfError)
                    {
                        fetchedMessage?.CommitTransaction();
                        Console.WriteLine($"Got an error message, but was ignored [byPassIfError = {byPassIfError}], this is an informational message only, no user action is required.");
                        fetchedMessage?.Dispose();
                    }
                    else
                    {
                        fetchedMessage?.AbortTransaction();
                        Console.WriteLine($"Got an error {ex.Message}");
                        fetchedMessage?.Dispose();

                        break;
                    }
                }
            }

            Console.ReadKey();
        }
    }
}
