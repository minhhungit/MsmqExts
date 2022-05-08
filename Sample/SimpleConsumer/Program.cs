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

            bool byPassIfError = true;
            bool ignoreMessageIfHasNoHandler = false;

            Console.WriteLine("fetching, please wait...");

            while (true)
            {
                IFetchedMessage message = null;
                string messageLabel = string.Empty;

                try
                {
                    message = msmqMessageQueue.Dequeue(_token);
                    messageLabel = message.Label;

                    switch (message.DequeueResultStatus)
                    {
                        case DequeueResultStatus.Success:
                            if (message.Result is ProductMessage prod)
                            {
                                var shortId = prod.Id.ToString().Substring(0, 7);
                                Console.WriteLine($"- got product in {Math.Round(message.DequeueElapsed.TotalMilliseconds, 2)}ms | [<{prod.Seq} / {shortId}> - {prod.CreatedDate.ToString("HH:mm:ss.fff")}]");

                                message?.CommitTransaction();
                                message?.Dispose();
                            }
                            else
                            {
                                throw new MsmqMessageHasNoHandlerException();
                            }
                            break;
                        case DequeueResultStatus.Timeout:
                            Console.WriteLine($"Dequeue got timeout ({msmqMessageQueue.Settings.ReceiveTimeout.TotalSeconds} seconds), this is an informational message only, no user action is required.");
                            break;
                        case DequeueResultStatus.Exception:
                            throw new Exception("Dequeue message got error: " + message.DequeueException?.Message);
                        default:
                            break;
                    }

                }
                catch (MsmqMessageHasNoHandlerException)
                {
                    if (ignoreMessageIfHasNoHandler)
                    {
                        message?.CommitTransaction();
                        Console.WriteLine($"Message has no handler, but was ignored [ignoreMessageIfHasNoHandler={ignoreMessageIfHasNoHandler}], message label is {messageLabel}, this is an informational message only, no user action is required.");
                        message?.Dispose();
                    }
                    else
                    {
                        message?.AbortTransaction();                        
                        Console.WriteLine($"Message has no handler, message label is {messageLabel}");
                        message?.Dispose();

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
                        message?.CommitTransaction();
                        Console.WriteLine($"Got an error message, but was ignored [byPassIfError = {byPassIfError}], this is an informational message only, no user action is required.");
                        message?.Dispose();
                    }
                    else
                    {
                        message?.AbortTransaction();
                        Console.WriteLine($"Got an error {ex.Message}");
                        message?.Dispose();

                        break;
                    }
                }
            }

            Console.ReadKey();
        }
    }
}
