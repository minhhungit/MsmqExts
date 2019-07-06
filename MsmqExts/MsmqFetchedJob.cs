// Inspired by https://www.hangfire.io/

using Newtonsoft.Json;
using System;

namespace MsmqExts
{
    public interface IFetchedJob : IDisposable
    {
        void RemoveFromQueue();
        void Requeue();
        object Result { get; }
    }

    public class MsmqFetchedJob : IFetchedJob
    {
        private readonly IMsmqTransaction _transaction;

        public MsmqFetchedJob(IMsmqTransaction transaction)
        {
            _transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
        }

        public MsmqFetchedJob(IMsmqTransaction transaction, string jsonBodyText, Type jsonTargetType)
        {
            _transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));

            JsonBodyText = jsonBodyText ?? throw new ArgumentNullException(nameof(jsonBodyText));
            JsonTargetType = jsonTargetType ?? throw new ArgumentNullException(nameof(jsonTargetType));
        }

        public string JsonBodyText { get; }
        public Type JsonTargetType { get; }

        public object Result
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(JsonBodyText) && JsonTargetType != null)
                {
                    return JsonConvert.DeserializeObject(JsonBodyText, JsonTargetType);
                }
                else
                {
                    return null;
                }
            }
        }

        public void RemoveFromQueue()
        {
            _transaction.Commit();
        }

        public void Requeue()
        {
            _transaction.Abort();
        }

        public void Dispose()
        {
            _transaction.Dispose();
        }
    }
}
