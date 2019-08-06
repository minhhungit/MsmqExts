using Newtonsoft.Json;
using System;

namespace MsmqExts
{
    public interface IFetchedJob : IDisposable
    {
        void Commit();
        void Abort();
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

        /// <summary>
        /// Message object
        /// </summary>
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

        public void Commit()
        {
            _transaction.Commit();
        }

        public void Abort()
        {
            _transaction.Abort();
        }

        public void Dispose()
        {
            _transaction.Dispose();
        }
    }
}
